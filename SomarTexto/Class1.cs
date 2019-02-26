using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;

namespace SomarTexto
{
    public class Class1
    {
        [CommandMethod("st", CommandFlags.Modal)]
        public void SomarTextos()
        {
            Editor ed = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Editor;
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            // Create a TypedValue array to define the filter criteria
            TypedValue[] tvs = new[]
            {
                new TypedValue(0, "TEXT"), // only DBText or MText
            };

            SelectionFilter filter = new SelectionFilter(tvs);
            PromptSelectionResult selection = ed.GetSelection(filter);

            if (selection.Status != PromptStatus.OK)
                return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                List<int> lista = new List<int>();

                foreach (SelectedObject obj in selection.Value)
                {
                    ObjectId id = obj.ObjectId;

                    DBText text = (DBText)tr.GetObject(id, OpenMode.ForRead);

                    int num = -1;
                    string numLadoEsquerdo = text.TextString.Split('+')[0];
                    string numLadoDireito = string.Empty;

                    try
                    {
                        numLadoDireito = text.TextString.Split('+')[1].Replace("*", "");
                    }
                    catch
                    {
                        // Faz nada...
                    }

                    if (int.TryParse(numLadoEsquerdo, out num))
                    {
                        lista.Add(Convert.ToInt32(numLadoEsquerdo));
                    }

                    if (int.TryParse(numLadoDireito, out num))
                    {
                        lista.Add(Convert.ToInt32(numLadoDireito));
                    }
                }

                int soma = 0;
                foreach (var item in lista)
                {
                    soma += item;
                }

                // Start a transaction
                using (Transaction acTrans = db.TransactionManager.StartTransaction())
                {
                    // Open the Block table for read
                    BlockTable acBlkTbl;
                    acBlkTbl = acTrans.GetObject(db.BlockTableId,
                                                    OpenMode.ForRead) as BlockTable;

                    // Open the Block table record Model space for write
                    BlockTableRecord acBlkTblRec;
                    acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                    OpenMode.ForWrite) as BlockTableRecord;

                    // Create a single-line text object
                    using (DBText acText = new DBText())
                    {
                        PromptPointResult pPtRes;
                        PromptPointOptions pPtOpts = new PromptPointOptions("");

                        // Prompt for the start point
                        pPtOpts.Message = "\nPonto de inserção do somatório: ";
                        pPtRes = doc.Editor.GetPoint(pPtOpts);
                        Point3d ptStart = pPtRes.Value;

                        if (soma != 0)
                        {
                            acText.Position = ptStart;
                            acText.Height = 35;
                            acText.TextString = soma.ToString();
                        }
                        else
                        {
                            ed.WriteMessage("\nNada a somar!");
                        }

                        acBlkTblRec.AppendEntity(acText);
                        acTrans.AddNewlyCreatedDBObject(acText, true);
                    }

                    // Save the changes and dispose of the transaction
                    acTrans.Commit();
                }

                tr.Commit();
            }
        }
    }
}
