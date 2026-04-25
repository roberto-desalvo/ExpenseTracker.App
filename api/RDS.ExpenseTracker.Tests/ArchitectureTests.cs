using System.Text.RegularExpressions;


namespace RDS.ExpenseTracker.ArchitectureTests
{
    public class ArchitectureTests
    {
        private readonly IObjectProvider<IType> PresentationLayer =
            Types().That().ResideInAssembly($@"^{Regex.Escape(typeof(PresentationAssemblyMarker).Assembly.GetName().Name)}(\..*)?$")
            .As("Presentation Layer");

        private readonly IObjectProvider<IType> InfrastructureLayer =
            Types().That().ResideInAssembly($@"^{Regex.Escape(typeof(InfrastructureAssemblyMarker).Assembly.GetName().Name)}(\..*)?$")
            .As("Infrastructure Layer");

        private readonly IObjectProvider<IType> ApplicationLayer =
            Types().That().ResideInAssembly($@"^{Regex.Escape(typeof(ApplicationAssemblyMarker).Assembly.GetName().Name)}(\..*)?$")
            .As("Application Layer");


        private static readonly Architecture Architecture = new ArchLoader()
            .LoadAssemblies(
                typeof(PresentationAssemblyMarker).Assembly,
                typeof(ApplicationAssemblyMarker).Assembly,
                typeof(InfrastructureAssemblyMarker).Assembly,
                typeof(DomainAssemblyMarker).Assembly)
            .Build();

        [Fact]
        public void PresentationLayer_ShouldNotDependOnDataLayer()
        {
            IArchRule rule = Types()
                .That().Are(PresentationLayer)
                .Should().NotDependOnAny(InfrastructureLayer)
                .Because("it's forbidden")
                .WithoutRequiringPositiveResults();

            rule.Check(Architecture);
        }

        [Theory]
        [InlineData(typeof(PresentationAssemblyMarker))]
        [InlineData(typeof(InfrastructureAssemblyMarker))]
        [InlineData(typeof(ApplicationAssemblyMarker))]
        [InlineData(typeof(DomainAssemblyMarker))]
        public void AllAssemblyClasses_ShouldResideInProperNamespace(System.Type type)
        {
            var assembly = type.Assembly;
            var expectedNamespacePrefix = assembly.GetName().Name;
            var namespaceRegexPattern = $@"^{Regex.Escape(expectedNamespacePrefix)}(\..*)?$";

            var rule = Types()
                .That().ResideInAssembly(assembly)
                .And().DoNotHaveName("Program")
                .Should().ResideInNamespace(namespaceRegexPattern, useRegularExpressions: true);

            rule.Check(Architecture);
        }
    }
}
