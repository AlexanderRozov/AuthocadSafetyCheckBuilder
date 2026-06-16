using System.Windows;

namespace databaserunner
{
    public class DataBaseDriver : IDatabaseRunner
    {
        private List<Element> elements = new List<Element>();
        public List<Element> Elements {  get; set; }
        private DataBaseDriver()
        {
            ReadFromDb();
        }

        private List<Element> ReadFromDb() { return elements;}
       
        public void AddElement(Element element)
        {
            try
            {

                Elements.Add(element);
                WriteToDb();
            }
            catch (Exception ex)
            {
               MessageBox.Show(ex.Message);
            }
        }

        public void DeleteElement(Guid guidToDelete)
        {
            try
            {
                foreach (Element el in elements)
                {
                    if (el.Id == guidToDelete)
                        Elements.Remove(el);
                    WriteToDb();
                    return;
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
           
        }
        public void UpdateElement(Guid guidToUpdate, Element element)
        {
            try
            {
                foreach (Element el in elements)
                {
                    if (el.Id == guidToUpdate)
                    {
                        el.ElementData = element.ElementData;
                        return;
                    }
                }
                WriteToDb();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void WriteToDb()
        {
            try
            {

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


    }
}
