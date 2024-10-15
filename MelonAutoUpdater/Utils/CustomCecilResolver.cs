using Mono.Cecil;
using System;
using System.Linq;

namespace MelonAutoUpdater.Utils
{
    internal class CustomCecilResolver : BaseAssemblyResolver
    {
        private readonly DefaultAssemblyResolver _defaultResolver;

        public CustomCecilResolver()
        {
            _defaultResolver = new DefaultAssemblyResolver();
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            AssemblyDefinition assembly = null;
            try
            {
                assembly = _defaultResolver.Resolve(name);
            }
            catch (AssemblyResolutionException)
            {
                var _assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var assemblies = _assemblies.Where(x => x.GetName().Name == name.Name);
                if (assemblies.Any())
                {
                    assembly = AssemblyDefinition.ReadAssembly(assemblies.First().GetFiles().First(), new ReaderParameters() { AssemblyResolver = new CustomCecilResolver() });
                }
            }
            return assembly;
        }
    }
}