using System.Text.Json;

namespace databaserunner
{
    public class Element
    {
       public Guid Id { get; set; }
       public JsonDocument ElementData {  get; set; }

    }
}
