using Demo.Abstractions;

namespace Demo.Services.Infrastructure
{
    public sealed class PtServices
    {
        public PtServices(PtDocumentState state)
        {
            Blocks = new PtBlockRepositoryImpl(state);
            Objects = new PtObjectRepositoryImpl(state, Blocks);
            Tables = new PtTableRepositoryImpl(state);
            DetectorZones = new PtDetectorZoneRepositoryImpl(state);
            PrecreatedTemplates = new PrecreatedTemplateRepositoryImpl(state);
        }

        public IPtObjectRepository Objects { get; }
        public IPtTableRepository Tables { get; }
        public IPtBlockRepository Blocks { get; }
        public IPtDetectorZoneRepository DetectorZones { get; }
        public IPrecreatedTemplateRepository PrecreatedTemplates { get; }
    }

    public static class PtServiceRegistry
    {
        private static readonly IDeviceCatalog DeviceCatalogInstance = new DeviceCatalogImpl();
        private static readonly IBlockCatalog BlockCatalogInstance = new BlockCatalogImpl();
        private static readonly ILayoutManager LayoutManagerInstance = new PtLayoutManagerImpl();

        public static PtServices Current => For(PtDocumentRegistry.Current);

        public static PtServices For(PtDocumentState state) => new PtServices(state);

        public static IDeviceCatalog DeviceCatalog => DeviceCatalogInstance;

        public static IBlockCatalog BlockCatalog => BlockCatalogInstance;

        public static ILayoutManager LayoutManager => LayoutManagerInstance;
    }
}
