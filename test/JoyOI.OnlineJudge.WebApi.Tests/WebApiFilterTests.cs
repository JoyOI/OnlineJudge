using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Moq;
using JoyOI.OnlineJudge.Models;
using JoyOI.OnlineJudge.WebApi.Controllers;

namespace JoyOI.OnlineJudge.WebApi.Tests
{
    public class WebApiFilterTests
    {
        public class TestClass
        {
            [WebApi(FilterLevel.PutDisabled)]
            public int A { get; set; }

            [WebApi(FilterLevel.PatchDisabled | FilterLevel.GetNeedRoot)]
            public string B { get; set; }

            [WebApi(FilterLevel.GetSingleDisabled | FilterLevel.GetListDisabled)]
            public double C { get; set; }
        }

        [Fact]
        public void Put_filter_tests()
        {
            var baseController = new BaseController();
            var entity = baseController.PutEntity<TestClass>("{ \"A\": 5, \"B\": \"123\", \"C\": 1.23 }");

            Assert.Equal(0, entity.A);
            Assert.Equal("123", entity.B);
            Assert.Equal(1.23, entity.C);
        }

        [Fact]
        public void Patch_filter_tests()
        {
            var baseController = new BaseController();
            var entity = new TestClass
            {
                A = 0,
                B = "Hello World",
                C = 123.456
            };

            baseController.PatchEntity(entity, "{ \"A\": 5, \"B\": \"123\", \"C\": 1.23 }");

            Assert.Equal(5, entity.A);
            Assert.Equal("Hello World", entity.B);
            Assert.Equal(1.23, entity.C);
        }

        [Fact]
        public void Get_single_without_root_tests()
        {
            var controller = new Mock<BaseController>();
            controller.Setup(x => x.IsRoot).Returns(false);
            var entity = new TestClass
            {
                A = 0,
                B = "Hello World",
                C = 123.456
            };
            controller.Object.FilterEntity(entity);

            Assert.Equal(null, entity.B);
            Assert.Equal(default(double), entity.C);
        }

        [Fact]
        public void Get_single_within_root_tests()
        {
            var controller = new Mock<BaseController>();
            controller.Setup(x => x.IsRoot).Returns(true);
            var entity = new TestClass
            {
                A = 0,
                B = "Hello World",
                C = 123.456
            };
            controller.Object.FilterEntity(entity);

            Assert.Equal("Hello World", entity.B);
            Assert.Equal(default(double), entity.C);
        }
    }
}
