﻿namespace EntityFramework.Functions.Tests.UnitTests
{
    using System;
    using System.Data.Entity.Core.Objects;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;

    using EntityFramework.Functions.Tests.Examples;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AdventureWorksTests
    {
        [TestMethod]
        public void StoredProcedureWithSingleResultTest()
        {
            using (AdventureWorks adventureWorks = new AdventureWorks())
            {
                ObjectResult<ManagerEmployee> employees = adventureWorks.uspGetManagerEmployees(2);
                Assert.IsTrue(employees.Any());
            }
        }

        [TestMethod]
        public void StoreProcedureWithOutParameterTest()
        {
            using (AdventureWorks adventureWorks = new AdventureWorks())
            {
                ObjectParameter errorLogId = new ObjectParameter("ErrorLogID", typeof(int)) { Value = 5 };
                int? rows = adventureWorks.LogError(errorLogId);
                Assert.AreEqual(0, errorLogId.Value);
                Assert.AreEqual(typeof(int), errorLogId.ParameterType);
                Assert.AreEqual(-1, rows);
            }
        }

        [TestMethod]
        public void StoreProcedureWithMultipleResultsTest()
        {
            using (AdventureWorks adventureWorks = new AdventureWorks())
            {
                // The first type of result type: a sequence of ProductCategory objects.
                ObjectResult<ProductCategory> categories = adventureWorks.uspGetCategoryAndSubCategory(1);
                Assert.IsNotNull(categories.Single());
                // The second type of result type: a sequence of ProductCategory objects.
                ObjectResult<ProductSubcategory> subcategories = categories.GetNextResult<ProductSubcategory>();
                Assert.IsTrue(subcategories.Any());
            }
        }

        [TestMethod]
        public void TableValuedFunctionTest()
        {
            using (AdventureWorks adventureWorks = new AdventureWorks())
            {
                IQueryable<ContactInformation> employees = adventureWorks.ufnGetContactInformation(1).Take(2);
                Assert.IsNotNull(employees.Single());
            }
        }

        [TestMethod]
        public void NonComposableScalarValuedFunctionTest()
        {
            using (AdventureWorks adventureWorks = new AdventureWorks())
            {
                decimal? cost = adventureWorks.ufnGetProductStandardCost(999, DateTime.Now);
                Assert.IsNotNull(cost);
                Assert.IsTrue(cost > 1);
            }
        }

        [TestMethod]
        public void NonComposableScalarValuedFunctionLinqTest()
        {
            using (AdventureWorks adventureWorks = new AdventureWorks())
            {
                try
                {
                    adventureWorks
                        .Products
                        .Where(product => product.ListPrice >= adventureWorks.ufnGetProductStandardCost(999, DateTime.Now))
                        .ToArray();
                    Assert.Fail();
                }
                catch (NotSupportedException exception)
                {
                    Trace.WriteLine(exception);
                }
            }
        }

        [TestMethod]
        public void ComposableScalarValuedFunctionLinqTest()
        {
            using (AdventureWorks adventureWorks = new AdventureWorks())
            {
                IQueryable<Product> products = adventureWorks
                    .Products
                    .Where(product => product.ListPrice <= adventureWorks.ufnGetProductListPrice(999, DateTime.Now));
                Assert.IsTrue(products.Any());
            }
        }

        [TestMethod]
        public void ComposableScalarValuedFunctionTest()
        {
            using (AdventureWorks adventureWorks = new AdventureWorks())
            {
                try
                {
                    adventureWorks.ufnGetProductListPrice(999, DateTime.Now);
                    Assert.Fail();
                }
                catch (NotSupportedException exception)
                {
                    Trace.WriteLine(exception);
                }
            }
        }

        [TestMethod]
        public void AggregateFunctionLinqTest()
        {
            using (AdventureWorks adventureWorks = new AdventureWorks())
            {
                var categories = adventureWorks.ProductSubcategories
                    .GroupBy(subcategory => subcategory.ProductCategoryID)
                    .Select(category => new
                    {
                        CategoryId = category.Key,
                        SubcategoryNames = category.Select(subcategory => subcategory.Name).Concat()
                    })
                    .ToArray();
                Assert.IsTrue(categories.Length > 0);
                categories.ForEach(category =>
                    {
                        Assert.IsTrue(category.CategoryId > 0);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(category.SubcategoryNames));
                    });
            }
        }

        [TestMethod]
        public void BuitInFunctionLinqTest()
        {
            using (AdventureWorks adventureWorks = new AdventureWorks())
            {
                var categories = adventureWorks.ProductSubcategories
                    .GroupBy(subcategory => subcategory.ProductCategoryID)
                    .Select(category => new
                    {
                        CategoryId = category.Key,
                        SubcategoryNames = category.Select(subcategory => subcategory.Name.Left(4)).Concat()
                    })
                    .ToArray();
                Assert.IsTrue(categories.Length > 0);
                categories.ForEach(category =>
                {
                    Assert.IsTrue(category.CategoryId > 0);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(category.SubcategoryNames));
                });
            }
        }

        [TestMethod]
        public void NiladicFunctionLinqTest()
        {
            using (AdventureWorks adventureWorks = new AdventureWorks())
            {
                var firstCategory = adventureWorks.ProductSubcategories
                    .GroupBy(subcategory => subcategory.ProductCategoryID)
                    .Select(category => new
                    {
                        CategoryId = category.Key,
                        SubcategoryNames = category.Select(subcategory => subcategory.Name.Left(4)).Concat(),
                        CurrentTimestamp = NiladicFunctions.CurrentTimestamp(),
                        CurrentUser = NiladicFunctions.CurrentUser(),
                        SessionUser = NiladicFunctions.SessionUser(),
                        SystemUser = NiladicFunctions.SystemUser(),
                        User = NiladicFunctions.User()
                    })
                    .First();
                Assert.IsNotNull(firstCategory);
                Assert.IsNotNull(firstCategory.CurrentTimestamp);
                Assert.IsTrue(DateTime.Now >= firstCategory.CurrentTimestamp);
                Assert.AreEqual("dbo", firstCategory.CurrentUser, true, CultureInfo.InvariantCulture);
                Assert.AreEqual("dbo", firstCategory.SessionUser, true, CultureInfo.InvariantCulture);
                Assert.AreEqual($@"{Environment.UserDomainName}\{Environment.UserName}", firstCategory.SystemUser, true, CultureInfo.InvariantCulture);
                Assert.AreEqual("dbo", firstCategory.User, true, CultureInfo.InvariantCulture);
            }
        }
    }
}
