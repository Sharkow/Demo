using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExcelHelperSH;
using Excel = Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;

namespace TestDataFIFO_DPAC_SorterSH
{
    class ExcelHandler
    {
        public const string
            DEFAULT_FIFO_EXP_START_CELL = "A4",
            DEFAULT_FIFO_EXP_END_CELL = "AG5",
            DEFAULT_FIFO_FACT_START_CELL = "AI4",
            DEFAULT_FIFO_FACT_END_CELL = "BO5",
            DEFAULT_DPAC_EXP_START_CELL = "A4",
            DEFAULT_DPAC_EXP_END_CELL = "AJ5",
            DEFAULT_DPAC_FACT_START_CELL = "AM4",
            DEFAULT_DPAC_FACT_END_CELL = "BV5";

        public static readonly string[]
            FIFO_MATCH_FIELDS = new string[] { "REF_NO_2", "ITEM", "TRAN_CODE", "ADJ_CODE", "COMP_ID", "UNITS", "TOTAL_COST_IFRS", "REF_NO_1" },
            DPAC_MATCH_FIELDS = new string[] { "REF_NO_2", "ITEM", "TRAN_CODE", "ADJ_CODE", "COMP_ID", "UNITS", "TOTAL_COST_DPAC", "DPA", "REF_NO_1" },
            FIFO_SF_LOC_MATCH_FIELDS = new string[] { "REF_NO_2", "ITEM", "TRAN_CODE", "ADJ_CODE", "COMP_ID", "ATTRIBUTE4", "UNITS", "TOTAL_COST_IFRS", "REF_NO_1" },
            DPAC_SF_LOC_MATCH_FIELDS = new string[] { "REF_NO_2", "ITEM", "TRAN_CODE", "ADJ_CODE", "COMP_ID", "ATTRIBUTE4", "UNITS", "TOTAL_COST_DPAC", "DPA", "REF_NO_1" };

        public const int
            FIFO_MATCH_FIELDS_MIN = 5,
            DPAC_MATCH_FIELDS_MIN = 5,
            FIFO_SF_LOC_MATCH_FIELDS_MIN = 6,
            DPAC_SF_LOC_MATCH_FIELDS_MIN = 6;


        private static TextBox expStartCell, expEndCell, factStartCell, factEndCell;
        private static Form appForm;
        private static Excel.Worksheet sheetToGetRangeFrom;
        private static int enteredCells = 0;


        public static void EnterRange(TextBox expStartCell, TextBox expEndCell, TextBox factStartCell, TextBox factEndCell, Form appForm)
        {
            Excel.Application excelApp = ExcelHelperSH.ExcelHelper.ConnectToRunningExcel();
            if (excelApp == null)
            {
                MessageBox.Show("Запущено несколько или ни одного приложения Excel.\nЗапустите требуемый файл, закройте остальные и запустите сортировку.");
                return;
            }

            ExcelHandler.expStartCell = expStartCell;
            ExcelHandler.expEndCell = expEndCell;
            ExcelHandler.factStartCell = factStartCell;
            ExcelHandler.factEndCell = factEndCell;
            ExcelHandler.appForm = appForm;

            if (sheetToGetRangeFrom != null)
            {
                try
                { sheetToGetRangeFrom.SelectionChange -= new Excel.DocEvents_SelectionChangeEventHandler(ExcelHandler_SelectionChange); }
                catch (Exception e)
                { }
            }

            enteredCells = 0;
            try
            {
                Excel.Workbook workbook = excelApp.ActiveWorkbook;
                sheetToGetRangeFrom = workbook.ActiveSheet;
            }
            catch (Exception e)
            {
                MessageBox.Show("Ошибка при подключении к Excel.");
                return;
            }
            sheetToGetRangeFrom.SelectionChange += new Excel.DocEvents_SelectionChangeEventHandler(ExcelHandler_SelectionChange);
        }

        static void ExcelHandler_SelectionChange(Excel.Range Target)
        {
            if (Target.Cells.Count > 1)
                return;

            switch (enteredCells)
            {
                case 0:
                    SetTextToTextBox(expStartCell, Target.Address.Replace(@"$", ""));
                    enteredCells++;
                    return;
                case 1:
                    SetTextToTextBox(expEndCell, Target.Address.Replace(@"$", ""));
                    enteredCells++;
                    return;
                case 2:
                    SetTextToTextBox(factStartCell, Target.Address.Replace(@"$", ""));
                    enteredCells++;
                    return;
                case 3:
                    SetTextToTextBox(factEndCell, Target.Address.Replace(@"$", ""));
                    enteredCells++;
                    Target.Worksheet.SelectionChange -= new Excel.DocEvents_SelectionChangeEventHandler(ExcelHandler_SelectionChange);
                    return;
                default:
                    return;
            }
        }

        static void SetTextToTextBox(TextBox tBox, string text)
        {
            appForm.Invoke((MethodInvoker)delegate
            {
                tBox.Text = text; // runs on UI thread
                tBox.Focus();
            });
        }

        public static void MakeSorting(string expStartCell, string expEndCell, string factStartCell, string factEndCell,
            string[] matchFields, bool useProgressiveAlgo, int matchingFieldsMinimum, double[] tolerances)
        {
            Excel.Application excelApp = ExcelHelperSH.ExcelHelper.ConnectToRunningExcel();
            if (excelApp == null)
            {
                MessageBox.Show("Запущено несколько или ни одного приложения Excel.\nЗапустите требуемый файл, закройте остальные и запустите сортировку.");
                return;
            }

            Excel.Workbook workbook;
            Excel.Worksheet worksheet;
            
            try
            {
                workbook = excelApp.ActiveWorkbook;
                worksheet = workbook.ActiveSheet;
            }
            catch (Exception e)
            {
                MessageBox.Show("Ошибка при подключении к Excel.");
                return;
            }

            if (!MakeSortingAtSheetSucess(worksheet.Name, workbook, expStartCell, expEndCell, factStartCell, factEndCell, matchFields, useProgressiveAlgo,
                matchingFieldsMinimum, tolerances))
                return;

            //MessageBox.Show("Сортировка завершена");
        }

        /// <summary>
        /// Сортирует строки на заданном листе.
        /// Возвращает true в случае успеха. В противном случае выполнение следующих процедур следует завершить.
        /// </summary>
        /// <param name="sheetName"></param>
        /// <returns></returns>
        private static bool MakeSortingAtSheetSucess(string sheetName, Excel.Workbook workbook,
            string expStartCell, string expEndCell, string factStartCell, string factEndCell,
            string[] matchFields, bool useProgressiveAlgo, int matchingFieldsMinimum, double[] tolerances)
        {   
            Excel.Worksheet sheet = ExcelHelper.FindSheetByName(sheetName, workbook);
            if (sheet == null)
            {
                MessageBox.Show("Не найден лист " + sheetName);
                return false;
            }

            //Фактическая область не может быть меньше ожидаемой по кол-ву строк!
            if (GetRowFromCellAdress(factStartCell) > GetRowFromCellAdress(expStartCell))
                factStartCell = GetColumnFromCellAddress(factStartCell) + GetRowFromCellAdress(expStartCell).ToString();

            if (GetRowFromCellAdress(factEndCell) < GetRowFromCellAdress(expEndCell))
                factEndCell = GetColumnFromCellAddress(factEndCell) + GetRowFromCellAdress(expEndCell).ToString();
            /////////////////////////////////////////////

            Excel.Range expRange = sheet.get_Range(expStartCell, expEndCell),
                factRange = sheet.get_Range(factStartCell, factEndCell);

            expRange.Interior.ColorIndex = 0;
            factRange.Interior.ColorIndex = 0;

            object[,] expRangeArr = expRange.Value2,
                factRangeArr = factRange.Value2;

            int[,] matchColumnIndecies = new int[2, matchFields.Length];

            for (int i = 0; i < matchFields.Length; i++)
            {
                matchColumnIndecies[0, i] = FindStringInRangeArrayFirstRow(expRangeArr, matchFields[i]);
                if (matchColumnIndecies[0, i] == -1)
                {
                    MessageBox.Show("На листе " + sheetName +
                        " в таблице с ожидаемыми результатами не найдено поле " + matchFields[i] + ".");
                    return false;
                }

                matchColumnIndecies[1, i] = FindStringInRangeArrayFirstRow(factRangeArr, matchFields[i]);
                if (matchColumnIndecies[0, i] == -1)
                {
                    MessageBox.Show("На листе " + sheetName +
                        " в таблице с фактическими результатами не найдено поле " + matchFields[i] + ".");
                    return false;
                }
            }
           
            if (useProgressiveAlgo)
                SortRowsProgressive(expRange, factRange, matchColumnIndecies, 1, matchingFieldsMinimum, tolerances);
            else
                SortStringsByPattern(expRange, factRange, matchColumnIndecies, 1, tolerances);

            return true;
        }


        private static int FindStringInRangeArrayFirstRow(object[,] rangeArr, string content)
        {
            if (content == null || content == "")
                return -1;

            for (int i = rangeArr.GetLowerBound(1); i <= rangeArr.GetUpperBound(1); i++)
                if (rangeArr[rangeArr.GetLowerBound(0), i] != null &&
                    rangeArr[rangeArr.GetLowerBound(0), i].ToString() == content)
                    return i;

            return -1;
        }

        /// <summary>
        /// Расставляет строки в массиве в соответствии с образцом.
        /// Возвращает пустую строку в случае успеха или сообщение об ошибке.
        /// </summary>
        /// <param name="rowsOffset">Начиная с какой строки делать сортировку массива.</param>
        private static void SortStringsByPattern(Excel.Range expRange, Excel.Range factRange, int[,] expAndFactIndecies, int rowsOffset, double[] tolerances)
        {
            object[,] expRangeArr = expRange.Value2;
            
            //ПОИСК ДУБЛИКАТОВ
            int duplicateCase = -1;
            //Array colors = Enum.GetValues(typeof(KnownColor));
            List<int> duplicateIndecies;
            List<int> allDuplicateIndecies = new List<int>();
            int[] columnIndeciesToMatch = new int[expAndFactIndecies.GetLength(1)];
            for (int i = 0; i < columnIndeciesToMatch.Length; i++)
                columnIndeciesToMatch[i] = expAndFactIndecies[0, i];
            for (int i = expRangeArr.GetLowerBound(0) + rowsOffset; i <= expRangeArr.GetUpperBound(0); i++)
            {
                if (allDuplicateIndecies.Contains(i)) continue;

                duplicateIndecies = new List<int>();
                for (int j = expRangeArr.GetLowerBound(0) + rowsOffset; j <= expRangeArr.GetUpperBound(0); j++)
                {
                    if (j == i || allDuplicateIndecies.Contains(j)) continue;

                    if (CompareArrayRowsAtColumns(expRangeArr, expRangeArr, i, j, columnIndeciesToMatch, columnIndeciesToMatch))
                    {
                        if (duplicateIndecies.Count == 0)
                        {
                            duplicateCase++;
                            duplicateIndecies.Add(i);
                            allDuplicateIndecies.Add(j);
                        }
                        duplicateIndecies.Add(j);
                        allDuplicateIndecies.Add(j);
                    }
                }
                if (duplicateIndecies.Count > 0)
                    foreach (int duplicateRow in duplicateIndecies)
                        ((Excel.Range)expRange.Rows[duplicateRow]).Interior.Color =
                            Color.FromArgb(Math.Max(240 - duplicateCase*20, 0), Math.Max(240 - duplicateCase*20, 0), 220);
            }
            /////////////////


            object[,] factRangeArr = factRange.Value2;
            bool found = false;
            bool missedRow = false;

            int[] expColumnIndeciesToMatch = new int[expAndFactIndecies.GetLength(1)],
                factColumnIndeciesToMatch = new int[expAndFactIndecies.GetLength(1)];
            for (int i = 0; i < expColumnIndeciesToMatch.Length; i++)
            {
                expColumnIndeciesToMatch[i] = expAndFactIndecies[0, i];
                factColumnIndeciesToMatch[i] = expAndFactIndecies[1, i];
            }

            for (int i = expRangeArr.GetLowerBound(0) + rowsOffset; i <= expRangeArr.GetUpperBound(0); i++)
            {
                if (allDuplicateIndecies.Contains(i)) continue;

                found = false;
                for (int j = factRangeArr.GetLowerBound(0) + rowsOffset; j <= factRangeArr.GetUpperBound(0); j++)
                {
                    if (CompareArrayRowsAtColumns(expRangeArr, factRangeArr, i, j, expColumnIndeciesToMatch, factColumnIndeciesToMatch, tolerances))
                    {
                        ExcelHelper.SwapRows(factRangeArr, i, j);
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    ((Excel.Range)expRange.Rows[i]).Interior.Color = Color.Red;
                    missedRow = true;
                }
            }
            factRange.Value2 = factRangeArr;

            if (allDuplicateIndecies.Count > 0)
                MessageBox.Show("Найдены дубликаты некоторых строк. Они были выделены цветами. Такие строки не участвовали в сортировке.");
            if (missedRow)
                MessageBox.Show("Не найдено строки в таблице с фактическими результатами, которая соответствует строке из таблицы с ожидаемыми. Строка выделена красным.");

            MessageBox.Show("Сортировка завершена.");
        }


        private static bool CompareArrayRowsAtColumns(object[,] arr1, object[,] arr2, int rowInd1, int rowInd2, int[] columnIndecies1, int[] columnIndecies2, double[] tolerances = null)
        {
            object arr1Value, arr2Value;
            string arr1Str, arr2Str;

            for (int i = 0; i < columnIndecies1.Length; i++)
            {
                arr1Value = arr1[rowInd1, columnIndecies1[i]];
                arr2Value = arr2[rowInd2, columnIndecies2[i]];

                arr1Str = arr1Value != null ? arr1Value.ToString() : "";
                arr2Str = arr2Value != null ? arr2Value.ToString() : "";

                if (tolerances != null && tolerances[i] > 0)//Сравение по данной колонке проводится учетом расхождения
                {
                    if (StringIsNumber(arr1Str) && StringIsNumber(arr2Str))
                    {
                        double arr1Num = arr1Str == "" ? 0 : double.Parse(arr1Str);
                        double arr2Num = arr2Str == "" ? 0 : double.Parse(arr2Str);

                        return (Math.Abs(arr1Num - arr2Num) <= tolerances[i]);
                    }
                    else
                    {
                        if (arr1Str != arr2Str)
                            return false;
                    }
                }
                else//Сравнение по колонке только по точному совпадению
                {
                    if (StringIsNumber(arr1Str) && StringIsNumber(arr2Str))
                    {
                        if ((arr1Str == "" ? 0 : double.Parse(arr1Str)) !=
                            (arr2Str == "" ? 0 : double.Parse(arr2Str)))
                            return false;
                    }
                    else
                    {
                        if (arr1Str != arr2Str)
                            return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Используется при прогрессивной сортировке.
        /// Сначала производится поиск уникальных строк в ожидаемой и фактической таблицах только по первой колонке для сравнения.
        /// Для строк, которые не уникальны, производится поиск по первой и второй колонке и т.д. (добавляются остальные колонки).
        /// </summary>
        /// <param name="expRange"></param>
        /// <param name="factRange"></param>
        /// <param name="expAndFactIndecies"></param>
        /// <param name="rowsOffset"></param>
        /// <returns></returns>
        private static void SortRowsProgressive(Excel.Range expRange, Excel.Range factRange, int[,] expAndFactIndecies, int rowsOffset,
            int matchingFieldsMinimum, double[] tolerances)
        {
            object[,] expRangeArr = expRange.Value2,
                factRangeArr = factRange.Value2;

            List<int> processedRowsExp = new List<int>(expRangeArr.GetLength(0));
            List<List<int>> duplicatesExp = null;
                //duplicatesFact = null;

            for (int columnsUsedToCompare = matchingFieldsMinimum; columnsUsedToCompare <= expAndFactIndecies.GetLength(1); columnsUsedToCompare++)
            {
                int[] expIndeciesUsed = new int[columnsUsedToCompare],
                    factIndeciesUsed = new int[columnsUsedToCompare];
                for (int i = 0; i < columnsUsedToCompare; i++)
                {
                    expIndeciesUsed[i] = expAndFactIndecies[0, i];
                    factIndeciesUsed[i] = expAndFactIndecies[1, i];
                }
                

                ////////////////////////////////////////Поиск дубликатов/////////////////////////////////////////////////////
                List<int> expRowsToProcess = new List<int>(expRangeArr.GetLength(0));
                duplicatesExp = new List<List<int>>();

                for (int i = expRangeArr.GetLowerBound(0) + rowsOffset; i <= expRangeArr.GetUpperBound(0); i++)
                {
                    if (processedRowsExp.Contains(i)) continue;
                    bool dupdup = false;
                    foreach (List<int> dup in duplicatesExp)
                        if (dup.Contains(i))
                        {
                            dupdup = true;
                            break;
                        }
                    if (dupdup) continue;

                    bool duplicatesFound = false;
                    for (int j = i+1; j <= expRangeArr.GetUpperBound(0); j++)
                        if (CompareArrayRowsAtColumns(expRangeArr, expRangeArr, i, j, expIndeciesUsed, expIndeciesUsed))
                        {
                            if (!duplicatesFound)
                            {
                                duplicatesExp.Add(new List<int>());
                                duplicatesExp.Last().Add(i);
                            }
                            duplicatesExp.Last().Add(j);
                            duplicatesFound = true;
                        }
                    if (!duplicatesFound)
                        expRowsToProcess.Add(i);
                }
                
                if (expRowsToProcess.Count == 0) continue;
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////


                for (int i = 0; i < expRowsToProcess.Count; i++)
                {
                    if (processedRowsExp.Contains(expRowsToProcess[i])) continue;
                    for (int j = factRangeArr.GetLowerBound(0); j <= factRangeArr.GetUpperBound(0); j++)
                    {
                        if (!processedRowsExp.Contains(j) && CompareArrayRowsAtColumns(expRangeArr, factRangeArr, expRowsToProcess[i], j,
                            expIndeciesUsed, factIndeciesUsed, tolerances))
                        {
                            ExcelHelper.SwapRows(factRangeArr, expRowsToProcess[i], j);
                            processedRowsExp.Add(expRowsToProcess[i]);
                            break;
                        }
                    }
                }

                if (processedRowsExp.Count == expRangeArr.GetLength(0))
                    break;
            }

            List<int> processedUnmatchedRows = new List<int>();

            if (processedRowsExp.Count < expRangeArr.GetLength(0) - rowsOffset)
            {
                //Пытаемся отсортировать строки, по которым есть несоответствия
                List<int> unprocessedExpRows = new List<int>(expRangeArr.GetLength(0) - rowsOffset - processedRowsExp.Count);

                for (int i = expRangeArr.GetLowerBound(0) + rowsOffset; i <= expRangeArr.GetUpperBound(0); i++)
                    if (!processedRowsExp.Contains(i))
                        unprocessedExpRows.Add(i);

                ////////////////////////////////////////////////////////////////////////
                processedUnmatchedRows = ProcessUnmatchedRows(expRangeArr, factRangeArr, unprocessedExpRows, expAndFactIndecies, rowsOffset, matchingFieldsMinimum, tolerances);
            }


            factRange.Value2 = factRangeArr;


            if (processedRowsExp.Count < expRangeArr.GetLength(0) - rowsOffset)//Если не все строки успешно отсортированы
            {
                if (duplicatesExp != null)
                for (int i = 0; i < duplicatesExp.Count; i++)
                    for (int j = 0; j < duplicatesExp[i].Count; j++)
                    {
                        ((Excel.Range)expRange.Rows[duplicatesExp[i][j]]).Interior.Color =
                                Color.FromArgb(Math.Max(240 - i * 20, 0), Math.Max(240 - i * 20, 0), 220);
                    }


                for (int i = expRangeArr.GetLowerBound(0) + rowsOffset; i <= expRangeArr.GetUpperBound(0); i++)
                    if (!processedRowsExp.Contains(i))
                    {
                        if (processedUnmatchedRows.Contains(i))
                            ((Excel.Range)expRange.Rows[i]).Interior.Color = Color.Yellow;
                        else
                        {
                            ((Excel.Range)expRange.Rows[i]).Interior.Color = Color.Red;
                            if (i <= factRangeArr.GetUpperBound(0) && !RowIsEmpty(factRangeArr, i))
                                ((Excel.Range)factRange.Rows[i]).Interior.Color = Color.FromArgb(255, 255, 128);
                        }
                    }

                MessageBox.Show("Сортировка завершена.\n\nКрасный цвет - строки, для которых не найдено соответствия в фактических данных.\nЖелтый цвет - строки, для которых установлены предположительные соответствия в фактических данных.\nОстальные цвета - дубликаты строк, для которых не найдено соответствий.\n\nЖелтый цвет в фактических данных - лишние транзакции.");
            }

                MessageBox.Show("Сортировка завершена.");
        }

        /// <summary>
        /// Пытается отсортировать строки, которые не имеют полного сходства.
        /// Возвращает список номеров строк в ожидаемых данных, для которых была найдена пара.
        /// </summary>
        /// <param name="expRangeArr"></param>
        /// <param name="factRangeArr"></param>
        /// <param name="unprocessedExpRows"></param>
        /// <param name="unprocessedFactRows"></param>
        /// <returns></returns>
        private static List<int> ProcessUnmatchedRows(object[,] expRangeArr, object[,] factRangeArr,
            List<int> unprocessedExpRows, int[,] expAndFactIndecies, int rowsOffset, int matchingFieldsMinimum, double[] tolerances)
        {
            if (unprocessedExpRows.Count == 0)
                return new List<int>();

            List<int> processedExpRows = new List<int>(unprocessedExpRows.Count);

            //Исключаем сначала по одному полю, потоп по 2 и т.д...
            for (int columnsUsedToCompare = expAndFactIndecies.GetLength(1); columnsUsedToCompare >= matchingFieldsMinimum; columnsUsedToCompare--)
            {
                int[] expIndeciesUsed = new int[columnsUsedToCompare],
                    factIndeciesUsed = new int[columnsUsedToCompare];
                for (int i = 0; i < columnsUsedToCompare; i++)
                {
                    expIndeciesUsed[i] = expAndFactIndecies[0, i];
                    factIndeciesUsed[i] = expAndFactIndecies[1, i];
                }

                for (int i = 0; i < unprocessedExpRows.Count; i++)
                    for (int j = factRangeArr.GetLowerBound(0) + rowsOffset; j <= factRangeArr.GetUpperBound(0); j++)
                    {
                        if (!unprocessedExpRows.Contains(j) && j <= expRangeArr.GetUpperBound(0))
                            continue;

                        if (CompareArrayRowsAtColumns(expRangeArr, factRangeArr, unprocessedExpRows[i], j,
                            expIndeciesUsed, factIndeciesUsed, tolerances))
                        {
                            ExcelHelper.SwapRows(factRangeArr, unprocessedExpRows[i], j);
                            processedExpRows.Add(unprocessedExpRows[i]);
                            unprocessedExpRows.RemoveAt(i);
                            i--;
                            if (unprocessedExpRows.Count == 0)
                                return processedExpRows;
                            break;
                        }
                    }
            }
            return processedExpRows;
        }


        private static bool StringIsNumber(string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                char ch = str[i];
                if (!((ch >= '0' && ch <= '9') ||
                    (ch == '-' && i == 0) || ch == ','))
                    return false;
            }

            return true;
        }

        private static bool RowIsEmpty(object[,] table, int row)
        {
            for (int i = table.GetLowerBound(1); i <= table.GetUpperBound(1); i++)
            {
                if (table[row, i] != null && table[row, i].ToString() != "")
                    return false;
            }

            return true;
        }

        private static string GetColumnFromCellAddress(string address)
        {
            for (int i = 0; i < address.Length; i++)
                if (address[i] >= '0' && address[i] <= '9')
                    return address.Substring(0, i);

            return address;
        }

        private static int GetRowFromCellAdress(string address)
        {
            for (int i = 0; i < address.Length; i++)
                if (address[i] >= '0' && address[i] <= '9')
                    return int.Parse(address.Substring(i));

            return int.Parse(address);
        }
    }
}
