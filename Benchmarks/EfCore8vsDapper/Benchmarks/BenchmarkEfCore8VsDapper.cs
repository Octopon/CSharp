using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using Dapper;
using EfCore8vsDapper.EfStructures;
using EfCore8vsDapper.Entities;
using EfCore8vsDapper.ViewModel;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace EfCore8vsDapper.Benchmarks
{
    public class AntiVirusFriendlyConfig : ManualConfig
    {       
        public AntiVirusFriendlyConfig()
        {
            AddJob(Job.MediumRun
                .WithToolchain(InProcessNoEmitToolchain.Instance));
        }
    }

    [Config(typeof(AntiVirusFriendlyConfig))]
    [MemoryDiagnoser(false)]
    [IterationCount(30)]
    public class BenchmarkEfCore8VsDapper
    {
        private PooledDbContextFactory<ApplicationDbContext> _pooledFactory;
        private const string CONNECTION = @"Server=DESKTOP-G9GINV5;Database=AdventureWorks2019;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False";

        [GlobalSetup]
        public void Setup()
        {
            var efCoreOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(CONNECTION).Options;
            _pooledFactory = new PooledDbContextFactory<ApplicationDbContext>(efCoreOptions);
        }

        #region 1Query-GetAllEntities 
        [Benchmark]
        //1 EfCore Tracking
        public List<SalesOrderDetail> EF_Tracking_GetSalesOrderDetails()
        {
            using var context = _pooledFactory.CreateDbContext();
            return context.SalesOrderDetails.ToList();
        }

        [Benchmark]
        //2 EfCore NoTracking
        public List<SalesOrderDetail> EF_NoTracking_GetSalesOrderDetails()
        {
            using var context = _pooledFactory.CreateDbContext();
            return context.SalesOrderDetails.AsNoTracking().ToList();
        }

        [Benchmark]
        //3 EfCore Query
        public List<SalesOrderDetail> EF_SQLQuery_GetSalesOrderDetails()
        {
            using var context = _pooledFactory.CreateDbContext();
            return context.Database.SqlQuery<SalesOrderDetail>
                (@$"
                SELECT 
                    SalesOrderID,
                    SalesOrderDetailID, 
                    CarrierTrackingNumber, 
                    OrderQty, 
                    ProductID, 
                    SpecialOfferID, 
                    UnitPrice,
                    UnitPriceDiscount, 
                    LineTotal, 
                    rowguid, 
                    ModifiedDate
                 FROM [Sales].[SalesOrderDetail] 
                ").ToList();
        }

        [Benchmark]
        //4 Dapper Query
        public List<SalesOrderDetail> Dapper_GetSalesOrderDetails()
        {
            using var sqlConnection = new SqlConnection(CONNECTION);
            sqlConnection.Open();

            return sqlConnection.Query<SalesOrderDetail>
                (@$"
                SELECT 
                    SalesOrderID,
                    SalesOrderDetailID, 
                    CarrierTrackingNumber, 
                    OrderQty, 
                    ProductID, 
                    SpecialOfferID, 
                    UnitPrice,
                    UnitPriceDiscount, 
                    LineTotal, 
                    rowguid, 
                    ModifiedDate
                 FROM [Sales].[SalesOrderDetail] 
                ").ToList();
        }
        #endregion

        #region 2Query-GetEntitiesByFilterAndSorting
        [Benchmark]
        //1 EfCore Tracking
        public List<SalesOrderDetail> EF_Tracking_GetSalesOrderDetailsByFilterAndSort()
        {
            using var context = _pooledFactory.CreateDbContext();
            return context.SalesOrderDetails
                    .Where(n => n.SalesOrderDetailID > 500 && n.SalesOrderDetailID < 110000 && n.UnitPrice > 20)
                    .OrderByDescending(n => n.ProductID).ToList();
        }

        [Benchmark]
        //2 EfCore NoTracking
        public List<SalesOrderDetail> EF_NoTracking_GetSalesOrderDetailsByFilterAndSort()
        {
            using var context = _pooledFactory.CreateDbContext();
            return context.SalesOrderDetails
                    .Where(n => n.SalesOrderDetailID > 500 && n.SalesOrderDetailID < 110000 && n.UnitPrice > 20)
                    .OrderByDescending(n => n.ProductID).AsNoTracking().ToList();
        }

        [Benchmark]
        //3 EfCore Query
        public List<SalesOrderDetail> EF_SqlQuery_GetSalesOrderDetailsByFilterAndSort()
        {
            using var context = _pooledFactory.CreateDbContext();
            return context.Database.SqlQuery<SalesOrderDetail>
                (@$"
                SELECT 
                    SalesOrderID,
                    SalesOrderDetailID, 
                    CarrierTrackingNumber, 
                    OrderQty, 
                    ProductID, 
                    SpecialOfferID, 
                    UnitPrice,
                    UnitPriceDiscount, 
                    LineTotal, 
                    rowguid, 
                    ModifiedDate
                 FROM [Sales].[SalesOrderDetail] 
                    WHERE [SalesOrderDetailID] > 500 AND [SalesOrderDetailID] < 110000 
                        AND [UnitPrice] > 20.0
                    ORDER BY [ProductID] DESC
                ").ToList();
        }

        [Benchmark]
        //4 Dapper Query
        public List<SalesOrderDetail> Dapper_GetSalesOrderDetailsByFilterAndSort()
        {
            using var sqlConnection = new SqlConnection(CONNECTION);
            sqlConnection.Open();
            return sqlConnection.Query<SalesOrderDetail>
                (@$"
                SELECT 
                    SalesOrderID,
                    SalesOrderDetailID, 
                    CarrierTrackingNumber, 
                    OrderQty, 
                    ProductID, 
                    SpecialOfferID, 
                    UnitPrice,
                    UnitPriceDiscount, 
                    LineTotal, 
                    rowguid, 
                    ModifiedDate
                 FROM [Sales].[SalesOrderDetail] 
                    WHERE [SalesOrderDetailID] > 500 AND [SalesOrderDetailID] < 110000 
                        AND [UnitPrice] > 20.0
                    ORDER BY [ProductID] DESC
                ").ToList();
        }
        #endregion

        #region 3Query-GetViewModels
        [Benchmark]
        //1 EfCore Tracking
        public List<OrderPriceVM> EF_Tracking_GetOrderPrices()
        {
            using var context = _pooledFactory.CreateDbContext();
            return context.SalesOrderDetails
                .GroupBy(m => new { m.SalesOrderID })
                .Select(n => new OrderPriceVM
                {
                    SalesOrderID = n.Key.SalesOrderID,
                    TotalOrderQty = n.Sum(m => m.OrderQty),
                    TotalPrice = n.Sum(m => m.OrderQty * (m.UnitPrice - m.UnitPriceDiscount)),
                    TotalDiscount = n.Sum(m => m.OrderQty * m.UnitPriceDiscount)
                })
                .ToList();
        }

        [Benchmark]
        //2 EfCore NoTracking
        public List<OrderPriceVM> EF_NoTracking_GetOrderPrices()
        {
            using var context = _pooledFactory.CreateDbContext();
            return context.SalesOrderDetails
                    .GroupBy(m => new { m.SalesOrderID })
                    .Select(n => new OrderPriceVM
                    {
                        SalesOrderID = n.Key.SalesOrderID,
                        TotalOrderQty = n.Sum(m => m.OrderQty),
                        TotalPrice = n.Sum(m => m.OrderQty * (m.UnitPrice - m.UnitPriceDiscount)),
                        TotalDiscount = n.Sum(m => m.OrderQty * m.UnitPriceDiscount)
                    })
                    .AsNoTracking()
                    .ToList();
        }

        [Benchmark]
        //3 EfCore Query
        public List<OrderPriceVM> EF_SqlQuery_GetAllOrderPrices()
        {
            using var context = _pooledFactory.CreateDbContext();
            return context.Database.SqlQuery<OrderPriceVM>
                   (@$"
                   SELECT  SalesOrderID, 
                           SUM(OrderQty) AS TotalOrderQty,
                           SUM(OrderQty * (UnitPrice - UnitPriceDiscount)) AS TotalPrice,
                           SUM(OrderQty * UnitPriceDiscount) AS TotalDiscount
                   FROM [Sales].[SalesOrderDetail] AS [s]
                       GROUP BY [s].[SalesOrderID]
                   ").ToList();
        }

        [Benchmark]
        //4 Dapper Query
        public List<OrderPriceVM> Dapper_GetAllOrderPrices()
        {
            using var sqlConnection = new SqlConnection(CONNECTION);
            sqlConnection.Open();
            return sqlConnection.Query<OrderPriceVM>
                    (@$"
                    SELECT  SalesOrderID, 
                           SUM(OrderQty) AS TotalOrderQty,
                           SUM(OrderQty * (UnitPrice - UnitPriceDiscount)) AS TotalPrice,
                           SUM(OrderQty * UnitPriceDiscount) AS TotalDiscount
                   FROM [Sales].[SalesOrderDetail] AS [s]
                       GROUP BY [s].[SalesOrderID]
                    ").ToList();
        }
        #endregion
    }
}
