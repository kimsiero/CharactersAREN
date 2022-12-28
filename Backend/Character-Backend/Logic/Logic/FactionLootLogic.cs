﻿using DAL.DALInterfaces;
using DAL.Exceptions;
using Logic.LogicInterfaces;
using Model.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Logic
{
    public class FactionLootLogic : IFactionLootLogic
    {
        private readonly IFactionLootRepository _repo;
        private readonly ILootProbabilityRepository _probabilityRepo;
        private readonly IItemGroupRepository _irRepo;

        public FactionLootLogic(IFactionLootRepository repo, ILootProbabilityRepository probabilityRepo, IItemGroupRepository irRepo)
        {
            _repo = repo;
            _probabilityRepo = probabilityRepo;
            _irRepo = irRepo;
        }

        public async Task<FactionLoot> DeleteFactionLoot(FactionLoot factionLoot)
        {
            return await _repo.DeleteEntity(factionLoot);
        }

        public async Task<FactionLoot> GetFactionLoot(int id)
        {
            
            #region todo remove, cuz this overrides the Get function yo
            /*
            Item IA = new Item();
            IA.Id = 1;
            IA.Name = "A";
            IA.Weight = 10;

            Item IB = new Item();
            IB.Id = 2;
            IB.Name = "B";
            IB.Weight = 1;

            ItemGroup IG = new ItemGroup();
            IG.Items = new List<Item>();
            IG.Items.Add(IA);
            IG.Items.Add(IB);

            LootProbability LA = new LootProbability();
            LA.ItemGroup = IG;
            LA.Probability = 0.8;
            LA.MinAmount = 1;
            LA.MaxAmount = 99;

            LootProbability LB = new LootProbability();
            LB.ItemGroup = IG;
            LB.Probability = 0.25;
            LB.MinAmount = 15;
            LB.MaxAmount = 20;

            FactionLoot f = new FactionLoot();
            f.LootProbabilities = new List<LootProbability>();
            f.LootProbabilities.Add(LA);
            f.LootProbabilities.Add(LB);
            return f;
            */
            #endregion
            
            var FactionLoot = await _repo.GetFactionLootById(id);

            if (FactionLoot != null)
            {
                foreach (LootProbability loot in FactionLoot.LootProbabilities)
                {
                    var itemGroup = await _irRepo.GetItemGroupById(loot.ItemGroup.Id);
                    FactionLoot.LootProbabilities.First(lp => lp.Id == loot.Id).ItemGroup = itemGroup;
                }
            }

            return FactionLoot;
        }

        public async Task<IEnumerable<FactionLoot>> GetFactionLoots()
        {
            return await _repo.GetFactionLoots();
        }

        public async Task<FactionLoot> PostFactionLoot(FactionLoot factionLoot)
        {
            var exExist = await _repo.FactionLootExists(factionLoot.Name);
            if (exExist)
            {
                throw new ObjectAlreadyExistsException(factionLoot.GetType().Name, factionLoot.Name);
            }
            else
            {
                return await _repo.AddEntity(factionLoot);
            }
        }

        public async Task<FactionLoot> PutFactionLoot(int id, FactionLoot factionLoot)
        {
            if (factionLoot.Id != id)
            {
                throw new ArgumentException($"{nameof(PutFactionLoot)}: id in url is not equal to FactionLoot id.");
            }

            //Updating the LootProbabilities before updating the FactionLoot
            foreach (LootProbability lootProbability in factionLoot.LootProbabilities)
            {
                var lootExists = await _probabilityRepo.LootProbabilityExists(lootProbability.ItemGroup.Id);
                if (lootExists)
                {
                    await _probabilityRepo.PutEntity(lootProbability);
                } 
                else
                {
                    var itemGroup = lootProbability.ItemGroup;
                    lootProbability.ItemGroup = null;
                    var newLoot = await _probabilityRepo.AddEntity(lootProbability);
                    newLoot.ItemGroup = itemGroup;
                    await _probabilityRepo.PutEntity(newLoot);
                    factionLoot.LootProbabilities.First(l => l.ItemGroup.Id == newLoot.ItemGroup.Id).Id = newLoot.Id;
                }
            }

            return await _repo.PutEntity(factionLoot);
        }

        public IEnumerable<Tuple<Item, int>> GenerateLoot(FactionLoot factionLoot)
        {
            Random r = new Random();

            ICollection<Item> loot = new List<Item>();

            foreach (LootProbability lootProbability in factionLoot.LootProbabilities)
            {
                if (r.NextDouble().CompareTo((double)lootProbability.Probability) <= 0)
                {
                    int count = r.Next(lootProbability.MinAmount, lootProbability.MaxAmount + 1);
                    for (int i = 0; i<count; i++)
                    {
                        loot.Add(lootProbability.ItemGroup.GenerateItem());
                    }
                }
            }
            return loot.GroupBy(x => x.Id)
            .Select(x => new Tuple<Item, int>(x.First(), x.Count()));
        }
    }
}
