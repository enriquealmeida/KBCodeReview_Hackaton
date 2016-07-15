using Artech.Architecture.Common.Objects;
using Artech.Architecture.Common.Services;
using Artech.Architecture.UI.Framework.Services;
using Artech.Genexus.Common;
using Artech.Genexus.Common.Objects;
using Artech.Genexus.Common.Parts;
using Artech.Genexus.Common.Helpers;
using System;
using System.IO;
using System.Text;
using Artech.Genexus.Common.Parts.SDT;
using System.Linq;
using System.Diagnostics;

namespace GUG.Packages.KBCodeRevisor
{
    static class KBCodeRevisorHelper
    {
        public static void ExportObjectInTextFormat()
        {
            IKBService kbserv = UIServices.KB;
            IOutputService output = CommonServices.Output;
            SpecificationListHelper helper = new SpecificationListHelper(kbserv.CurrentModel.Environment.TargetModel);

            string title = "KBCodeRevisor - Generate objects in text format";
            output.StartSection(title);

            string newDir = KBCodeRevisorDirectory(kbserv) + @"\";
            SelectObjectOptions selectObjectOption = new SelectObjectOptions();
            selectObjectOption.MultipleSelection = true;

            foreach (KBObject obj in UIServices.SelectObjectDialog.SelectObjects(selectObjectOption))
            {
                        output.AddLine(obj.GetFullName());
                        WriteObjectToTextFile(obj, newDir);
            }

            bool success = true;
            output.EndSection(title, success);
        }

 

        private static void WriteObjectToTextFile(KBObject obj, string newDir)
        {

            string name = ReplaceInvalidCharacterInFileName(obj.GetFullName());

            string FileName = newDir + name + ".txt";

            System.IO.StreamWriter file = new System.IO.StreamWriter(FileName);

            file.WriteLine("======OBJECT = " + name + " === " + obj.Description + "=====");

            RulesPart rp = obj.Parts.Get<RulesPart>();
            if (rp != null)
            {
                file.WriteLine("=== RULES ===");
                file.WriteLine(rp.Source);
            }

            switch (obj.TypeDescriptor.Name)
            {

                case "Attribute":

                    Artech.Genexus.Common.Objects.Attribute att = (Artech.Genexus.Common.Objects.Attribute)obj;

                    file.WriteLine(Functions.ReturnPicture(att));
                    if (att.Formula == null)
                        file.WriteLine("");
                    else
                        file.WriteLine(att.Formula.ToString());
                    break;

                case "Procedure":
                    ProcedurePart pp = obj.Parts.Get<ProcedurePart>();
                    if (pp != null)
                    {
                        file.WriteLine("=== PROCEDURE SOURCE ===");
                        file.WriteLine(pp.Source);
                    }
                    break;
                case "Transaction":
                    StructurePart sp = obj.Parts.Get<StructurePart>();
                    if (sp != null)
                    {
                        file.WriteLine("=== STRUCTURE ===");
                        file.WriteLine(sp.ToString());
                    }

                    EventsPart ep = obj.Parts.Get<EventsPart>();
                    if (ep != null)
                    {
                        file.WriteLine("=== EVENTS SOURCE ===");
                        file.WriteLine(ep.Source);
                    }
                    break;

                case "WorkPanel":
                    WorkPanel wkp = (WorkPanel)obj;

                    ep = obj.Parts.Get<EventsPart>();
                    if (ep != null)
                    {
                        file.WriteLine("=== EVENTS SOURCE ===");
                        file.WriteLine(ep.Source);
                    }
                    break;

                case "WebPanel":

                    WebPanel wbp = (WebPanel)obj;
                    ep = obj.Parts.Get<EventsPart>();
                    if (ep != null)
                    {
                        file.WriteLine("=== EVENTS SOURCE ===");
                        file.WriteLine(ep.Source);
                    }
                    break;


                case "WebComponent":

                    wbp = (WebPanel)obj;
                    ep = obj.Parts.Get<EventsPart>();
                    if (ep != null)
                    {
                        file.WriteLine("=== EVENTS SOURCE ===");
                        file.WriteLine(ep.Source);
                    }
                    break;

                case "Table":
                    Table tbl = (Table)obj;

                    foreach (TableAttribute attr in tbl.TableStructure.Attributes)
                    {
                        String line = "";
                        if (attr.IsKey)
                        {
                            line = "*";
                        }
                        else
                        {
                            line = " ";
                        }

                        line += attr.Name + "  " + attr.GetPropertiesObject().GetPropertyValueString("DataTypeString") + "-" + attr.GetPropertiesObject().GetPropertyValueString("Formula");

                        if (attr.IsExternalRedundant)
                            line += " External_Redundant";

                        line += " Null=" + attr.IsNullable;
                        if (attr.IsRedundant)
                            line += " Redundant";

                        file.WriteLine(line);
                    }
                    break;


                case "SDT":
                    SDT sdtToList = (SDT)obj;
                    if (sdtToList != null)
                    {
                        file.WriteLine("=== STRUCTURE ===");
                        ListStructure(sdtToList.SDTStructure.Root, 0, file);
                    }
                    break;

                default:

                    //Unknown object. Use export format.
                    file.Write(SerializeObject(obj).ToString());
                    break;


            }
            file.Close();
        }

        private static StringBuilder SerializeObject(KBObject obj)
        {
            StringBuilder buffer = new StringBuilder();
            using (TextWriter writer = new StringWriter(buffer))
                obj.Serialize(writer);
            return buffer;
        }

        private static string ReplaceInvalidCharacterInFileName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            string invalidCharsRemoved = new string(name
            .Where(x => !invalidChars.Contains(x))
            .ToArray());
            name = name.Replace("'", "");
            name = name.Replace(":", "_");
            name = name.Replace(" ", "");
            name = name.Replace(@"\", "_");
            name = name.Replace("/", "_");
            return name;
        }

        private static void ListStructure(SDTLevel level, int tabs, System.IO.StreamWriter file)
        {
            WriteTabs(tabs, file);
            file.Write(level.Name);
            if (level.IsCollection)
                file.Write(", collection: {0}", level.CollectionItemName);
            file.WriteLine();

            foreach (var childItem in level.GetItems<SDTItem>())
                ListItem(childItem, tabs + 1, file);
            foreach (var childLevel in level.GetItems<SDTLevel>())
                ListStructure(childLevel, tabs + 1, file);
        }


        private static void ListItem(SDTItem item, int tabs, System.IO.StreamWriter file)
        {
            WriteTabs(tabs, file);
            string dataType = item.Type.ToString().Substring(0, 1) + "(" + item.Length.ToString() + (item.Decimals > 0 ? "." + item.Decimals.ToString() : "") + ")" + (item.Signed ? "-" : "");
            file.WriteLine("{0}, {1}, {2} {3}", item.Name, dataType, item.Description, (item.IsCollection ? ", collection " + item.CollectionItemName : ""));
        }

        private static void WriteTabs(int tabs, System.IO.StreamWriter file)
        {
            while (tabs-- > 0)
                file.Write('\t');
        }



        public static void OpenFolderKBCodeRevisor()
        {
            Process.Start(KBCodeRevisorDirectory(UIServices.KB));
        }


        public static string SpcDirectory(IKBService kbserv)
        {
            GxModel gxModel = kbserv.CurrentKB.DesignModel.Environment.TargetModel.GetAs<GxModel>();
            return kbserv.CurrentKB.Location + string.Format(@"\GXSPC{0:D3}\", gxModel.Model.Id);
        }

        public static string KBCodeRevisorDirectory(IKBService kbserv)
        {
            GxModel gxModel = kbserv.CurrentKB.DesignModel.Environment.TargetModel.GetAs<GxModel>();
            string dir = Path.Combine(SpcDirectory(kbserv), "KBCodeRevisor");
            try
            {
                Directory.CreateDirectory(dir);
            }
            catch (Exception) { }

            return dir;
        }

   
    }
}