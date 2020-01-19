using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace NamedKeyDIDemo
{
    public class FunctionalNamedServiceProviderDemo
    {
        [Fact]
        public void Test_Temperature_Mapper_Directly()
        {
            var sut = ServiceBuilder.Get();

            var converterMapper = sut.GetRequiredService<KelvinConverterMapper>();
            var mappings = _testCases.Select(tc =>
                (tc.Input, tc.ExpectedResult, ActualResult: converterMapper(tc.TempScale, tc.Input)));
            var tolerance = new Func<decimal, decimal>(d => Math.Round(d, 2, MidpointRounding.ToEven));

            foreach (var (_, expectedResult, actualResult) in mappings)
            {
                Assert.Equal(tolerance(expectedResult), tolerance(actualResult));
            }
        }

        [Fact]
        public void Test_Temperature_Mapper_Via_Class()
        {
            var sut = ServiceBuilder.Get();

            var converterMapper = sut.GetRequiredService<TemperatureCalculator>();

            var mappings = _testCases.Select(tc =>
                (tc.Input, tc.ExpectedResult, ActualResult: converterMapper.GetTemperatureInKelvins(tc.TempScale, tc.Input)));
            var tolerance = new Func<decimal, decimal>(d => Math.Round(d, 2, MidpointRounding.ToEven));

            foreach (var (_, expectedResult, actualResult) in mappings)
            {
                Assert.Equal(tolerance(expectedResult), tolerance(actualResult));
            }
        }

        private static Lazy<(string TempScale, decimal Input, decimal ExpectedResult)[]> _testCasesInitializer = new Lazy<(string TempScale, decimal Input, decimal ExpectedResult)[]>(
            () =>
                new (string TempScale, decimal Input, decimal ExpectedResult)[]
                {
                    ("Centigrade", 0, 273.15M),
                    ("Centigrade", 100, 373.15M),
                    ("Fahrenheit", 0, 255.37M),
                    ("Fahrenheit", 32, 273.15M),
                    ("Fahrenheit", 100, 310.928M),
                    ("Rankin", 0, 0),
                    ("Rankin", 250, 138.89M),
                    ("Kelvin", 0, 0M),
                    ("Kelvin", 100.12M, 100.12M),
                });

        private static (string TempScale, decimal Input, decimal ExpectedResult)[] _testCases =
            _testCasesInitializer.Value;

        public class TemperatureCalculator
        {
            private readonly KelvinConverterMapper _mapperLookup;

            public TemperatureCalculator(KelvinConverterMapper mapperLookup)
            {
                _mapperLookup = mapperLookup;
            }

            public decimal GetTemperatureInKelvins(string scale, decimal value)
            {
                return _mapperLookup(scale, value);
            }
        }

        public delegate decimal KelvinConverterMapper(string tempScale, decimal temp);
        
        private static class ServiceBuilder
        {
            private static readonly Lazy<IServiceProvider> Instance = new Lazy<IServiceProvider>(GetServiceProvider);

            public static IServiceProvider Get() => Instance.Value;

            private static IServiceProvider GetServiceProvider()
            {
                var services = new ServiceCollection();

                services.AddSingleton<KelvinConverterMapper>(provider => (tempScale, temp) =>
                {
                    switch ((string.IsNullOrEmpty(tempScale) ? " " : tempScale).ToUpper()[0])
                    {
                        case 'C':
                            return CentigradeToKelvins(temp);

                        case 'F':
                            return FahrenheitToKelvins(temp);

                        case 'R':
                            return RankineToKelvinMapper(temp);

                        case 'K':
                            return KelvinsToKelvins(temp);

                        default:
                            throw new KeyNotFoundException($"Unknown scale '{tempScale}'");
                    }

                });

                services.AddSingleton<TemperatureCalculator>();

                return services.BuildServiceProvider();
            }

            private static decimal CentigradeToKelvins(decimal value) => value + 273.15M;
           
            private static decimal FahrenheitToKelvins(decimal value) => (value + 459.67M) * 5 / 9;

            private static decimal RankineToKelvinMapper (decimal value) => value * 5 / 9;

            private static decimal KelvinsToKelvins(decimal value) => value;
            
        }
    }
}