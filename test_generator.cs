using AStar.Dev.Source.Generators.OptionsBindingGeneration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
var config = new ConfigurationBuilder().Build();
services.AddAutoRegisteredOptions(config);
