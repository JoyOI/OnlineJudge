using System.Linq;
using Xunit;
using Moq;
using JoyOI.OnlineJudge.Models;
using JoyOI.OnlineJudge.WebApi.Controllers;

namespace JoyOI.OnlineJudge.WebApi.Tests
{
    public class TestModel
    {
        [WebApi(FilterLevel.PutDisabled)]
        public int A { get; set; }

        [WebApi(FilterLevel.PatchDisabled | FilterLevel.GetNeedRoot)]
        public string B { get; set; }

        [WebApi(FilterLevel.GetSingleDisabled | FilterLevel.GetListDisabled)]
        public double C { get; set; }
    }

    public class WebApiFilterTests
    {
        [Fact]
        public void Put_filter_tests()
        {
            var baseController = new BaseController();
            var entity = baseController.PutEntity<TestModel>("{ \"A\": 5, \"B\": \"123\", \"C\": 1.23 }").Entity;

            Assert.Equal(0, entity.A);
            Assert.Equal("123", entity.B);
            Assert.Equal(1.23, entity.C);
        }

        [Fact]
        public void Patch_filter_tests()
        {
            var baseController = new BaseController();
            var entity = new TestModel
            {
                A = 7,
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
            var entity = new TestModel
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
            var entity = new TestModel
            {
                A = 0,
                B = "Hello World",
                C = 123.456
            };
            controller.Object.FilterEntity(entity);

            Assert.Equal("Hello World", entity.B);
            Assert.Equal(default(double), entity.C);
        }

        [Fact]
        public void Get_patched_property_names()
        {
            var baseController = new BaseController();
            var entity = new TestModel
            {
                A = 0,
                B = "Hello World",
                C = 123.456
            };

            var json = "{ \"a\": 24680 }";
            var changes = baseController.PatchEntity(entity, json);

            Assert.Equal(1, changes.Count());
            Assert.Equal("A", changes.First());
        }

        [Fact]
        public void Get_put_property_names()
        {
            var baseController = new BaseController();
            var fields = baseController.PutEntity<TestModel>("{ \"A\": 5, \"b\": \"123\" }").Fields;

            Assert.Equal(1, fields.Count());
            Assert.Equal("B", fields.First());
        }
    }
}
