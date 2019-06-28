using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TestDataGeneratorSH.DataProviders;
using TestDataGeneratorSH.OutputGeneration;
using TestDataGeneratorSH.ValueGenerators;
using TestDataGeneratorSH.ValueGenerators.Sensors;
using TestDataGeneratorSH.ValueGenerators.StaticData;
using System.Runtime.Serialization;
using System.IO;
using System.Xml;

namespace TestDataGeneratorSH.Controls
{
    /// <summary>
    /// Главное окно приложения.
    /// </summary>
    public partial class MainForm : Form
    {
        #region Fields
        List<DPPanel<Sensor, SensorField, SensorValGeneratorBase, SensorFieldPanel>> sensorPanels
            = new List<DPPanel<Sensor, SensorField, SensorValGeneratorBase, SensorFieldPanel>>();

        List<DPPanel<StaticData, StaticDataField, StaticDataValGeneratorBase, StaticDataFieldPanel>> staticDataPanels
            = new List<DPPanel<StaticData, StaticDataField, StaticDataValGeneratorBase, StaticDataFieldPanel>>();

        IntervalController intervalController;

        string lastSaveDirectory = Application.StartupPath + @"\";

        //Если пользователь вышел из приложения во время генерации, её нужно отменить, а затем закрыть форму.
        bool quitOnGenerationComplete = false;
        #endregion


        #region Public Methods
        public MainForm()
        {
            InitializeComponent();

            intervalController = new IntervalController(genTimeBar, genTimeVal, genTimeTypeChooser);
            intervalController.IntervalMS = 100000;
            intervalController.IntervalChanged += new IntervalController.IntervalControllerHandler(intervalController_IntervalChanged);

            startTimePicker.CustomFormat = Preferences.UIGlobal.DATE_TIME_FORMAT;
            startTimePicker.Value = DateTime.Now;

            DataGenRoutine.AvailableSeedNamesForTypes = new string[Enum.GetValues(typeof(DataGenRoutine.SensorFieldTypeTitles)).Length][];
            DataGenRoutine.GenerationComplete += new DataGenRoutine.DataGenCompleteHandler(GenerationComplete);
        }
        #endregion


        #region Private Methods

        private void addSensorBtn_Click(object sender, EventArgs e)
        {
            AddSensor();
        }

        private void addSDataBtn_Click(object sender, EventArgs e)
        {
            AddStaticData();
        }

        void AddSensor(Sensor dataProvider = null)
        {
            SensorPanel newSensorPanel = new SensorPanel(dataProvider);
            newSensorPanel.IntervalController.MaxMS = intervalController.IntervalMS;
            newSensorPanel.SizeChanged += new EventHandler
                (PositionDPPanelsAfter<Sensor, SensorField, SensorValGeneratorBase, SensorFieldPanel>);
            sensorPanels.Add(newSensorPanel);
            PositionDPPanelsAfter<Sensor, SensorField, SensorValGeneratorBase, SensorFieldPanel>
                (sensorPanels.Count > 1 ? sensorPanels[sensorPanels.Count - 2] : null);
            newSensorPanel.DataProvider.TitleChanged += new DataProviderBase.DPHandler
                (CheckDPUniqueness<Sensor, SensorField, SensorValGeneratorBase, SensorFieldPanel>);
            newSensorPanel.DeleteRequest += new DPPanel<Sensor, SensorField, SensorValGeneratorBase, SensorFieldPanel>.DPPanelHandler
                (DeletePanel<Sensor, SensorField, SensorValGeneratorBase, SensorFieldPanel>);
            newSensorPanel.DataProvider.CheckStateChanged += new DataProviderBase.DPHandler(CheckSetValidity);
            newSensorPanel.DataProvider.ValidChanged += new DataProviderBase.DPHandler(CheckSetValidity);
            newSensorPanel.DataProvider.UpdateIntervalChanged += new DataProviderBase.DPHandler(DataProvider_UpdateIntervalChanged);

            newSensorPanel.DataProvider.FieldAdded += new FieldContainerBase<SensorField, SensorValGeneratorBase>.FieldAddedHandler
                (DataProvider_FieldAdded);
            newSensorPanel.DataProvider.FieldRemoved += new DataProviderBase.DPHandler(UpdateAvailableSeeds);
            newSensorPanel.DataProvider.TitleChanged += new DataProviderBase.DPHandler(UpdateAvailableSeeds);
            newSensorPanel.DataProvider.ValidChanged += new DataProviderBase.DPHandler(UpdateAvailableSeeds);
            newSensorPanel.DataProvider.CheckStateChanged += new DataProviderBase.DPHandler(UpdateAvailableSeeds);

            sensorsContainer.Controls.Add(newSensorPanel);
            CheckSetValidity();
        }

        void DataProvider_UpdateIntervalChanged(DataProviderBase sender)
        {
            ((Sensor)sender).UpdateIntervalTest =
                ((Sensor)sender).UpdateIntervalMS <= intervalController.IntervalMS;
        }

        void DataProvider_FieldAdded<FieldType, FieldValueGeneratorBase>
            (FieldContainerBase<FieldType, FieldValueGeneratorBase> sender, FieldType field)
            where FieldType : FieldBase<FieldValueGeneratorBase>, new()
            where FieldValueGeneratorBase : ValueGeneratorBase
        {
            field.OutputTypeChanged += new DataProviderBase.DPHandler(UpdateAvailableSeeds);
            field.TitleChanged += new DataProviderBase.DPHandler(UpdateAvailableSeeds);
            UpdateAvailableSeeds();
        }

        void AddStaticData(StaticData dataProvider = null)
        {
            StaticDataPanel newPanel = new StaticDataPanel(dataProvider);
            newPanel.SizeChanged += new EventHandler
                (PositionDPPanelsAfter<StaticData, StaticDataField, StaticDataValGeneratorBase, StaticDataFieldPanel>);
            staticDataPanels.Add(newPanel);
            PositionDPPanelsAfter<StaticData, StaticDataField, StaticDataValGeneratorBase, StaticDataFieldPanel>
                (staticDataPanels.Count > 1 ? staticDataPanels[staticDataPanels.Count - 2] : null);
            newPanel.DataProvider.TitleChanged += new DataProviderBase.DPHandler
                (CheckDPUniqueness<StaticData, StaticDataField, StaticDataValGeneratorBase, StaticDataFieldPanel>);
            newPanel.DeleteRequest += new DPPanel<StaticData, StaticDataField, StaticDataValGeneratorBase, StaticDataFieldPanel>.
                DPPanelHandler(DeletePanel<StaticData, StaticDataField, StaticDataValGeneratorBase, StaticDataFieldPanel>);
            newPanel.DataProvider.CheckStateChanged += new DataProviderBase.DPHandler(CheckSetValidity);
            newPanel.DataProvider.ValidChanged += new DataProviderBase.DPHandler(CheckSetValidity);

            newPanel.DataProvider.FieldAdded += new FieldContainerBase<StaticDataField, StaticDataValGeneratorBase>.FieldAddedHandler
                (DataProvider_FieldAdded);
            newPanel.DataProvider.FieldRemoved += new DataProviderBase.DPHandler(UpdateAvailableSeeds);
            newPanel.DataProvider.TitleChanged += new DataProviderBase.DPHandler(UpdateAvailableSeeds);
            newPanel.DataProvider.ValidChanged += new DataProviderBase.DPHandler(UpdateAvailableSeeds);
            newPanel.DataProvider.CheckStateChanged += new DataProviderBase.DPHandler(UpdateAvailableSeeds);

            staticDataContainer.Controls.Add(newPanel);
            CheckSetValidity();
        }

        void DeletePanel<FieldContainerType, FieldType, FieldValueGeneratorBase, FieldPanelType>
            (DPPanel<FieldContainerType, FieldType, FieldValueGeneratorBase, FieldPanelType> sender)
            where FieldContainerType : FieldContainerBase<FieldType, FieldValueGeneratorBase>, new()
            where FieldPanelType : DPFieldPanel<FieldType, FieldValueGeneratorBase>, new()
            where FieldType : FieldBase<FieldValueGeneratorBase>, new()
            where FieldValueGeneratorBase : ValueGeneratorBase
        {
            List<DPPanel<FieldContainerType, FieldType, FieldValueGeneratorBase, FieldPanelType>> panelList =
                GetPanelCollection<FieldContainerType, FieldType, FieldValueGeneratorBase, FieldPanelType>();

            Panel panelContainer = null;
            if (sender is SensorPanel)
                panelContainer = sensorsContainer;
            else if (sender is StaticDataPanel)
                panelContainer = staticDataContainer;

            for (int i = panelList.IndexOf(sender) + 1; i < panelList.Count; i++)
                panelList[i].Location = new Point(panelList[i].Location.X,
                    panelList[i].Location.Y - (sender).Height - Preferences.DPPanels.DP_PANEL_Y_OFFSET);
            panelContainer.Controls.Remove(sender);
            panelList.Remove(sender);
            CheckDPUniqueness<FieldContainerType, FieldType, FieldValueGeneratorBase, FieldPanelType>();
            UpdateAvailableSeeds();
            CheckSetValidity();
        }

        /// <summary>
        /// Positions all panels that go after "sender".
        /// </summary>
        /// <param name="sender">Pass null here to position all panels.</param>
        /// <param name="e"></param>
        void PositionDPPanelsAfter<FieldContainerType, FieldType, FieldValueGeneratorBase, FieldPanelType>
            (object sender = null, EventArgs e = null)
            where FieldContainerType : FieldContainerBase<FieldType, FieldValueGeneratorBase>, new()
            where FieldPanelType : DPFieldPanel<FieldType, FieldValueGeneratorBase>, new()
            where FieldType : FieldBase<FieldValueGeneratorBase>, new()
            where FieldValueGeneratorBase : ValueGeneratorBase
        {
            List<DPPanel<FieldContainerType, FieldType, FieldValueGeneratorBase, FieldPanelType>> sortPanels =
                GetPanelCollection<FieldContainerType, FieldType, FieldValueGeneratorBase, FieldPanelType>();

            if (sortPanels.Count == 0)
                return;
            int senderIndex = 0;
            if (sender == null)
                sortPanels[0].Location = new Point(Preferences.DPPanels.DP_PANEL_X_OFFSET,
                    Preferences.DPPanels.DP_PANEL_Y_OFFSET);
            else
                senderIndex = sortPanels.IndexOf
                    ((DPPanel<FieldContainerType, FieldType, FieldValueGeneratorBase, FieldPanelType>)sender);
            for (int i = senderIndex + 1; i < sortPanels.Count; i++)
                sortPanels[i].Location = new Point(Preferences.DPPanels.DP_PANEL_X_OFFSET,
                    sortPanels[i - 1].Bottom + Preferences.DPPanels.DP_PANEL_Y_OFFSET);
        }

        void CheckDPUniqueness<FieldContainerType, FieldType, FieldValueGeneratorBase, FieldPanelType>
            (object sender = null)
            where FieldContainerType : FieldContainerBase<FieldType, FieldValueGeneratorBase>, new()
            where FieldPanelType : DPFieldPanel<FieldType, FieldValueGeneratorBase>, new()
            where FieldType : FieldBase<FieldValueGeneratorBase>, new()
            where FieldValueGeneratorBase : ValueGeneratorBase
        {
            List<DPPanel<FieldContainerType, FieldType, FieldValueGeneratorBase, FieldPanelType>> panelList =
                GetPanelCollection<FieldContainerType, FieldType, FieldValueGeneratorBase, FieldPanelType>();
            List<int> duplicatedIndecies = new List<int>(panelList.Count);

            for (int i = 0; i < panelList.Count; i++)
            {
                if (duplicatedIndecies.Contains(i))
                    continue;
                for (int j = i + 1; j < panelList.Count; j++)
                {
                    if (panelList[i].DataProvider.Title == panelList[j].DataProvider.Title)
                    {
                        duplicatedIndecies.Add(i);
                        duplicatedIndecies.Add(j);
                    }
                }
            }

            IEnumerable<int> distinctIndexies = duplicatedIndecies.Distinct();

            for (int i = 0; i < panelList.Count; i++)
                panelList[i].DataProvider.TitleUniqueTest = !distinctIndexies.Contains(i);
        }

        void intervalController_IntervalChanged(IntervalController sender)
        {
            foreach (SensorPanel sPanel in sensorPanels)
            {
                sPanel.IntervalController.MaxMS = intervalController.IntervalMS;
                sPanel.DataProvider.UpdateIntervalTest =
                    sPanel.DataProvider.UpdateIntervalMS <= intervalController.IntervalMS;
            }
        }
        
        /// <summary>
        /// Returns the appropriate collection of panels depending on panel type.
        /// </summary>
        List<DPPanel<FieldContainerType, FieldType, FieldValueGeneratorBase, FieldPanelType>> GetPanelCollection
            <FieldContainerType, FieldType, FieldValueGeneratorBase, FieldPanelType>()
            where FieldContainerType : FieldContainerBase<FieldType, FieldValueGeneratorBase>, new()
            where FieldPanelType : DPFieldPanel<FieldType, FieldValueGeneratorBase>, new()
            where FieldType : FieldBase<FieldValueGeneratorBase>, new()
            where FieldValueGeneratorBase : ValueGeneratorBase
        {
            Object obj = null;
            if (sensorPanels is List<DPPanel<FieldContainerType, FieldType, FieldValueGeneratorBase, FieldPanelType>>)
                obj = sensorPanels;
            else if (staticDataPanels is List<DPPanel<FieldContainerType, FieldType, FieldValueGeneratorBase, FieldPanelType>>)
                obj = staticDataPanels;
            else
                throw new Exception("Invalid panel type passed to MainForm.GetPanelCollection");

            return (List<DPPanel<FieldContainerType, FieldType, FieldValueGeneratorBase, FieldPanelType>>)obj;
        }

        void sensorsPanel_Click(object sender, EventArgs e)
        {
            ((Control)sender).Focus();
        }

        /// <summary>
        /// Устанавливает список доступных полей, которые доступны как входные поля для генераторов,
        /// для каждого типа данных.
        /// </summary>
        /// <param name="sender"></param>
        void UpdateAvailableSeeds(Object sender = null)
        {
            DataGenRoutine.SensorFieldTypeTitles[] typeArr =
                (DataGenRoutine.SensorFieldTypeTitles[])Enum.GetValues(typeof(DataGenRoutine.SensorFieldTypeTitles));
            string[][] res = new string[typeArr.Length][];
            List<string> currList;
            for (int i = 0; i < typeArr.Length; i++)
            {
                currList = new List<string>();
                foreach (SensorPanel sensorPanel in sensorPanels)
                {
                    if (!sensorPanel.DataProvider.Checked)//Свойство Valid можно не проверять. Даже если сенсор содержит ошибки,
                        //его можно считать доступным параметром.
                        //Если учитывать свойство Valid, то при каждом добавлении поля в датчик будет сбиваться всё на свете...
                        continue;
                    foreach (SensorField field in sensorPanel.DataProvider.Fields)
                        if (field.OutputType == typeArr[i])
                            currList.Add("<Sensor> " + sensorPanel.DataProvider.Title + " " + field.Title);
                }
                foreach (StaticDataPanel sDataPanel in staticDataPanels)
                {
                    if (!sDataPanel.DataProvider.Checked)
                        continue;
                    foreach (StaticDataField field in sDataPanel.DataProvider.Fields)
                        if (field.OutputType == typeArr[i])
                            currList.Add("<Static Data> " + sDataPanel.DataProvider.Title + " " + field.Title);
                }
                res[i] = currList.ToArray();
            }
            DataGenRoutine.AvailableSeedNamesForTypes = res;
        }

        /// <summary>
        /// Проверка правильности всех введенных пользователем данных.
        /// Определяет доступность генерации данных.
        /// </summary>
        /// <param name="sender"></param>
        void CheckSetValidity(DataProviderBase sender = null)
        {
            if (sensorPanels.Count == 0 && staticDataPanels.Count == 0)
            {
                runButton.Enabled = false;
                return;
            }
            foreach (SensorPanel sPanel in sensorPanels)
                if (sPanel.DataProvider.Checked && !(sPanel.DataProvider.Valid))
                {
                    runButton.Enabled = false;
                    return;
                }
            foreach (StaticDataPanel sdPanel in staticDataPanels)
                if (sdPanel.DataProvider.Checked && !sdPanel.DataProvider.Valid)
                {
                    runButton.Enabled = false;
                    return;
                }

            foreach (SensorPanel sPanel in sensorPanels)//Check if there are any checked sensors/static data structures
                if (sPanel.DataProvider.Checked)
                {
                    runButton.Enabled = true;
                    return;
                }
            foreach (StaticDataPanel sdPanel in staticDataPanels)
                if (sdPanel.DataProvider.Checked)
                {
                    runButton.Enabled = true;
                    return;
                }
            runButton.Enabled = false;
        }




        ///////////////////////GENERATION HANDLING//////////////////////////////////

        private void runButton_Click(object sender, EventArgs e)
        {
            List<Sensor> sensors = new List<Sensor>(sensorPanels.Count);
            List<StaticData> staticData = new List<StaticData>(staticDataPanels.Count);
            
            foreach (DPPanel<Sensor, SensorField, SensorValGeneratorBase, SensorFieldPanel> sensor in sensorPanels)
                if (sensor.DataProvider.Checked)
                    sensors.Add(sensor.DataProvider);

            foreach (DPPanel<StaticData, StaticDataField, StaticDataValGeneratorBase, StaticDataFieldPanel>
                sData in staticDataPanels)
                if (sData.DataProvider.Checked)
                    staticData.Add(sData.DataProvider);


            progressBar.Visible = true;
            genCancelBtn.Visible = true;
            toolStrip1.Enabled = false;
            settingsContainer.Enabled = false;
            sensorsContainer.Enabled = false;
            staticDataContainer.Enabled = false;
            this.FormClosing += new FormClosingEventHandler(CancelGenerationBeforeClose);
            this.Cursor = Cursors.AppStarting;

            DataGenRoutine.Generate(sensors, staticData, startTimePicker.Value, intervalController.IntervalMS,
                progressBar, outputDate.Checked, outputTime.Checked, outputMS.Checked,
                outputSensorFieldTitles.Checked, outputStatDataFieldTitles.Checked, outputStructureToFile.Checked);
        }

        void GenerationComplete(bool canceled)
        {
            this.FormClosing -= new FormClosingEventHandler(CancelGenerationBeforeClose);

            if (quitOnGenerationComplete)//Даже если флаг cancel == false,
                //запрос на отмену генерации всё-равно мог быть сделан, поэтому если quitOnGenerationComplete установлен
                //в true, то закрываем форму в любом случае.
                this.Close();
            
            progressBar.Visible = false;
            genCancelBtn.Visible = false;
            toolStrip1.Enabled = true;
            settingsContainer.Enabled = true;
            sensorsContainer.Enabled = true;
            staticDataContainer.Enabled = true;
            this.Cursor = Cursors.Default;
        }

        void CancelGenerationBeforeClose(object sender, FormClosingEventArgs e)
        {
            //Просто не даём пользователю выйти
            e.Cancel = true;

            /*if (MessageBox.Show("Запущена генерация. Прервать её?", "SH Test Data Generator - выход",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
            {
                quitOnGenerationComplete = true;
                this.Enabled = false;
                DataGenRoutine.CancelGeneration();
            }*/
        }

        void RequestCancelGeneration(object sender, EventArgs e)
        {
            DataGenRoutine.CancelGeneration();
        }




        //___________________________СЕРИАЛИЗАЦИЯ______________________________________________
        ///////////////////////////////////////////////////////////////////////////////////////

        private void saveButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog sDial = new SaveFileDialog();

            int ind = lastSaveDirectory.LastIndexOf(@"\") + 1;
            sDial.InitialDirectory = lastSaveDirectory.Substring(0, ind);
            if (ind >= lastSaveDirectory.Length)
                sDial.FileName = "";
            else
                sDial.FileName = lastSaveDirectory.Substring(ind, lastSaveDirectory.Length - ind);

            sDial.Filter = "(*.xml)|*.xml";
            sDial.OverwritePrompt = true;
            sDial.Title = "SH TestDataGenerator - Сохранить конфигурацию";
            if (sDial.ShowDialog() == DialogResult.Cancel)
                return;

            lastSaveDirectory = sDial.FileName;
            XmlTextWriter outStream = new XmlTextWriter(File.Create(sDial.FileName), Preferences.Output.OUTPUT_ENCODING);
            outStream.Formatting = Formatting.Indented;


            Application.UseWaitCursor = true;
            this.Enabled = false;
            Application.DoEvents();

            List<Sensor> sensors = new List<Sensor>(sensorPanels.Count);
            List<StaticData> staticData = new List<StaticData>(staticDataPanels.Count);

            foreach (SensorPanel sensorPanel in sensorPanels)
                sensors.Add(sensorPanel.DataProvider);

            foreach (StaticDataPanel staticDataPanel in staticDataPanels)
                staticData.Add(staticDataPanel.DataProvider);

            DataGenConfiguration genConfig = new DataGenConfiguration(sensors, staticData, intervalController.IntervalMS, startTimePicker.Value,
                outputDate.Checked, outputTime.Checked, outputMS.Checked, outputSensorFieldTitles.Checked, outputStructureToFile.Checked, outputStatDataFieldTitles.Checked);

            DataContractSerializer dataContractSerializer = new DataContractSerializer(typeof(DataGenConfiguration), DataGenRoutine.KnownGenerationTypes);
            dataContractSerializer.WriteObject(outStream, genConfig);
            outStream.Close();

            Application.UseWaitCursor = false;
            this.Enabled = true;
        }


        private void loadButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog oDial = new OpenFileDialog();
            int ind = lastSaveDirectory.LastIndexOf(@"\") + 1;
            oDial.InitialDirectory = lastSaveDirectory.Substring(0, ind);
            oDial.Filter = "(*.xml)|*.xml";
            oDial.Title = "SH TestDataGenerator - Загрузить конфигурацию";
            oDial.CheckFileExists = true;

            if (oDial.ShowDialog() == DialogResult.Cancel)
                return;

            lastSaveDirectory = oDial.FileName;
            XmlTextReader inStream = new XmlTextReader(oDial.FileName);


            Application.UseWaitCursor = true;
            this.Enabled = false;
            Application.DoEvents();


            DataContractSerializer dataContractSerializer = new DataContractSerializer(typeof(DataGenConfiguration), DataGenRoutine.KnownGenerationTypes);
            DataGenConfiguration genConfig = (DataGenConfiguration)dataContractSerializer.ReadObject(inStream);
            inStream.Close();

           
            //Очистить текущий список датчиков
            foreach (SensorPanel sensorPanel in sensorPanels)
                sensorsContainer.Controls.Remove(sensorPanel);
            sensorPanels.Clear();

            foreach (StaticDataPanel staticDataPanel in staticDataPanels)
                staticDataContainer.Controls.Remove(staticDataPanel);
            staticDataPanels.Clear();


            //Устанавливаем простые свойства формы
            intervalController.IntervalMS = genConfig.IntervalMS;
            outputDate.Checked = genConfig.OutputDate;
            outputTime.Checked = genConfig.OutputTime;
            outputMS.Checked = genConfig.OutputMS;
            outputSensorFieldTitles.Checked = genConfig.OutputSensorFieldTitles;
            outputStructureToFile.Checked = genConfig.OutputStructureToFile;
            outputStatDataFieldTitles.Checked = genConfig.OutputStaticDataFieldTitles;
            startTimePicker.Value = genConfig.StartDateTime;

            //Добавляем панели датчиков и статических данных
            foreach (Sensor sensor in genConfig.Sensors)
                AddSensor(sensor);
            foreach (StaticData staticData in genConfig.StaticData)
                AddStaticData(staticData);

            //Восстанавливаем список доступных сидов
            UpdateAvailableSeeds();


            Application.UseWaitCursor = false;
            this.Enabled = true;
        }
        #endregion
    }
}
