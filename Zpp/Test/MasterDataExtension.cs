using System;
using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.Context;
using Master40.DB.DataModel;

namespace Zpp.Test
{
    public class MasterDataExtension
    {
        public static void ExtendByDesk(ProductionDomainContext productionDomainContext)
        {
            // Article Types
            var articleTypes = new M_ArticleType[]
            {
                new M_ArticleType {Name = "Assembly"},
                new M_ArticleType {Name = "Material"},
                new M_ArticleType {Name = "Consumable"}
            };
            productionDomainContext.ArticleTypes.AddRange(articleTypes);
            productionDomainContext.SaveChanges();

            // Units
            var units = new M_Unit[]
            {
                new M_Unit {Name = "Kilo"},
                new M_Unit {Name = "Litre"},
                new M_Unit {Name = "Pieces"}
            };
            productionDomainContext.Units.AddRange(units);
            productionDomainContext.SaveChanges();

            var machines = new M_Machine[]
            {
                //new Machine{Capacity=1, Name="Säge", Count = 1, MachineGroup = new MachineGroup{ Name = "Zuschnitt" } },
                //new Machine{Capacity=1, Name="Bohrer", Count = 1, MachineGroup = new MachineGroup{ Name = "Bohrwerk" } },
                
                // Verpacken
                new M_Machine
                    {Capacity = 1, Name = "Montage 1", Count = 1, MachineGroup = new M_MachineGroup {Name = "Montage"}},
                // Schweißen
                new M_Machine
                    {Capacity = 1, Name = "Montage 2", Count = 1, MachineGroup = new M_MachineGroup {Name = "Montage"}}
            };
            productionDomainContext.Machines.AddRange(machines);
            productionDomainContext.SaveChanges();

            var machineTools = new M_MachineTool[]
            {
                new M_MachineTool
                {
                    MachineId = machines.Single(m => m.Name == "Montage 2").Id, SetupTime = 1,
                    Name = "Schweißgerät"
                }
            };
            productionDomainContext.MachineTools.AddRange(machineTools);
            productionDomainContext.SaveChanges();

            // Articles
            var articles = new M_Article[]
            {
                new M_Article
                {
                    Name = "Tisch", ArticleTypeId = articleTypes.Single(s => s.Name == "Assembly").Id,
                    CreationDate = DateTime.Parse("2016-09-01"), DeliveryPeriod = 20,
                    UnitId = units.Single(s => s.Name == "Pieces").Id, Price = 45.00, ToBuild = true, ToPurchase = false
                },
                new M_Article
                {
                    Name = "Tischplatte", ArticleTypeId = articleTypes.Single(s => s.Name == "Consumable").Id,
                    DeliveryPeriod = 10, UnitId = units.Single(s => s.Name == "Pieces").Id, Price = 10.00,
                    ToBuild = false, ToPurchase = true
                },
                new M_Article
                {
                    Name = "Tischbein", ArticleTypeId = articleTypes.Single(s => s.Name == "Assembly").Id,
                    DeliveryPeriod = 10, UnitId = units.Single(s => s.Name == "Pieces").Id, Price = 5.00,
                    ToBuild = true, ToPurchase = false
                },
                new M_Article
                {
                    Name = "Schrauben", ArticleTypeId = articleTypes.Single(s => s.Name == "Consumable").Id,
                    CreationDate = DateTime.Parse("2005-09-01"), DeliveryPeriod = 3,
                    UnitId = units.Single(s => s.Name == "Pieces").Id, Price = 0.05, ToBuild = false, ToPurchase = true
                },
                // Anschraubplatte, Stahlrohr, Montageanleitung
                new M_Article
                {
                    Name = "Anschraubplatte", ArticleTypeId = articleTypes.Single(s => s.Name == "Consumable").Id,
                    CreationDate = DateTime.Parse("2019-07-31"), DeliveryPeriod = 3,
                    UnitId = units.Single(s => s.Name == "Pieces").Id, Price = 1.00, ToBuild = false, ToPurchase = true
                },
                new M_Article
                {
                    Name = "Stahlrohr", ArticleTypeId = articleTypes.Single(s => s.Name == "Consumable").Id,
                    CreationDate = DateTime.Parse("2019-07-31"), DeliveryPeriod = 3,
                    UnitId = units.Single(s => s.Name == "Pieces").Id, Price = 2.00, ToBuild = false, ToPurchase = true
                },
                new M_Article
                {
                    Name = "Montageanleitung", ArticleTypeId = articleTypes.Single(s => s.Name == "Consumable").Id,
                    CreationDate = DateTime.Parse("2019-07-31"), DeliveryPeriod = 1,
                    UnitId = units.Single(s => s.Name == "Pieces").Id, Price = 0.5, ToBuild = false, ToPurchase = true
                },
                
            };
            productionDomainContext.Articles.AddRange(articles);
            productionDomainContext.SaveChanges();

            // get the name -> id mappings
            var DBArticles = productionDomainContext
                .Articles
                .ToDictionary(p => p.Name, p => p.Id);


            // create Stock Entrys for each Article
            foreach (var article in DBArticles)
            {
                var stocks = new M_Stock[]
                {
                    new M_Stock
                    {
                        ArticleForeignKey = article.Value,
                        Name = "Stock: " + article.Key,
                        Min = (article.Key == "Schrauben") ? 50 : 0,
                        Max = 100,
                        Current = (article.Key == "Schrauben") ? 100 : 0
                    }
                };
                productionDomainContext.Stocks.AddRange(stocks);
                productionDomainContext.SaveChanges();
            }

            var articleBom = new List<M_ArticleBom>
            {
                // Tisch
                new M_ArticleBom {ArticleChildId = articles.Single(a => a.Name == "Tisch").Id, Name = "Tisch"},
                new M_ArticleBom
                {
                    ArticleChildId = articles.Single(a => a.Name == "Tischplatte").Id, Name = "Tischplatte",
                    Quantity = 1, ArticleParentId = articles.Single(a => a.Name == "Tisch").Id
                },
                new M_ArticleBom
                {
                    ArticleChildId = articles.Single(a => a.Name == "Tischbein").Id, Name = "Tischbein", Quantity = 4,
                    ArticleParentId = articles.Single(a => a.Name == "Tisch").Id
                },
                new M_ArticleBom
                {
                    ArticleChildId = articles.Single(a => a.Name == "Schrauben").Id, Name = "Schrauben", Quantity = 16,
                    ArticleParentId = articles.Single(a => a.Name == "Tisch").Id
                },
                new M_ArticleBom
                {
                    ArticleChildId = articles.Single(a => a.Name == "Montageanleitung").Id, Name = "Montageanleitung", Quantity = 1,
                    ArticleParentId = articles.Single(a => a.Name == "Tisch").Id
                },

                // Tischbein
                new M_ArticleBom
                {
                    ArticleChildId = articles.Single(a => a.Name == "Anschraubplatte").Id, Name = "Anschraubplatte",
                    Quantity = 1, ArticleParentId = articles.Single(a => a.Name == "Tischbein").Id
                },
                new M_ArticleBom
                {
                    ArticleChildId = articles.Single(a => a.Name == "Stahlrohr").Id, Name = "Stahlrohr",
                    Quantity = 1, ArticleParentId = articles.Single(a => a.Name == "Tischbein").Id
                },
            };
            productionDomainContext.ArticleBoms.AddRange(articleBom);
            productionDomainContext.SaveChanges();

            var workSchedule = new M_Operation[]
            {
                // Tisch 
                new M_Operation
                {
                    ArticleId = articles.Single(a => a.Name == "Tisch").Id, Name = "Tisch Verpackung", Duration = 10,
                    MachineGroupId = machines.Single(n => n.Name == "Montage 1").MachineGroupId, HierarchyNumber = 10
                },
                // Tischbeine 
                new M_Operation
                {
                    ArticleId = articles.Single(a => a.Name == "Tischbein").Id, Name = "Schweißen", Duration = 5,
                    MachineGroupId = machines.Single(n => n.Name == "Montage 2").MachineGroupId, HierarchyNumber = 10
                },
                // new WorkSchedule{ ArticleId = articles.Single(a => a.Name == "Tischbein").Id, Name = "Löcher vorbohren", Duration=2, MachineGroupId=machines.Single(n=> n.Name=="Montage 1").MachineGroupId, HierarchyNumber = 20 },
            };
            productionDomainContext.Operations.AddRange(workSchedule);
            productionDomainContext.SaveChanges();


            // TODO
            //create Businesspartner
            var businessPartner = new M_BusinessPartner()
                {Debitor = true, Kreditor = false, Name = "Toys'R'us Spielwarenabteilung"};
            var businessPartner2 = new M_BusinessPartner()
                {Debitor = false, Kreditor = true, Name = "Teile Großhandel"};
            productionDomainContext.BusinessPartners.Add(businessPartner);
            productionDomainContext.BusinessPartners.Add(businessPartner2);
            productionDomainContext.SaveChanges();

            var articleToBusinessPartner = new M_ArticleToBusinessPartner[]
            {
                new M_ArticleToBusinessPartner
                {
                    BusinessPartnerId = businessPartner2.Id,
                    ArticleId = articles.Single(x => x.Name == "Holz 1,5m x 3,0m").Id, PackSize = 1, Price = 1,
                    DueTime = 2
                },
            };
            productionDomainContext.ArticleToBusinessPartners.AddRange(articleToBusinessPartner);
            productionDomainContext.SaveChanges();
            
        }

        public static void CreateCustomerOrdersWithDesks(ProductionDomainContext productionDomainContext, int businessPartnerId)
        {
            var order = new T_CustomerOrder
            {
                BusinessPartnerId = businessPartnerId,
                CreationTime = 10,
                Name = "BeispielOrder 1",
                DueTime = 30
            };
            productionDomainContext.Add(order);
            productionDomainContext.SaveChanges();
            var orderPart = new T_CustomerOrderPart
            {
                ArticleId = 1,
                Quantity = 1,
                CustomerOrderId = order.Id
            };
            productionDomainContext.Add(orderPart);
            productionDomainContext.SaveChanges();
        }
    }
}