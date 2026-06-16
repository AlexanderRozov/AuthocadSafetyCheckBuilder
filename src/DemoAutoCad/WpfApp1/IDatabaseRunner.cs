namespace databaserunner
{
    public interface IDatabaseRunner
    {
        public void AddElement(Element element);
        public void DeleteElement(Guid guid);
        public void UpdateElement(Guid guid, Element element);

        public void WriteToDb();
    }
}
