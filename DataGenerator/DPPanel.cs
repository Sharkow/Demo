using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TestDataGeneratorSH.DataProviders;
using TestDataGeneratorSH.ValueGenerators;

namespace TestDataGeneratorSH.Controls
{
    /// <summary>
    /// Базовый класс для панелей настройки датчиков и статических структур данных.
    /// Дизайн определён в суперклассе - DPPanelUI.
    /// Generic-параметры определяются взависимости от того, что настраивает панель - датчик или статические данные.
    /// </summary>
    /// <typeparam name="FieldContainerType">Тип объекта, для настройки которого создаётся панель - датчик или статическая структура данных.
    /// Остальные Generic-параметры устанавливаются соответственно выбранному FieldContaierType.</typeparam>
    public abstract class DPPanel<FieldContainerType, FieldType, FieldValueGeneratorBase, FieldPanelType> : DPPanelUI
        where FieldContainerType : FieldContainerBase<FieldType, FieldValueGeneratorBase>, new()
        where FieldPanelType : DPFieldPanel<FieldType, FieldValueGeneratorBase>, new()
        where FieldType : FieldBase<FieldValueGeneratorBase>, new()
        where FieldValueGeneratorBase : ValueGeneratorBase
    {
        public delegate void DPPanelHandler(DPPanel<FieldContainerType, FieldType, FieldValueGeneratorBase, FieldPanelType> sender);

        /// <summary>
        /// Sent when user requests to delete the sensor.
        /// </summary>
        public event DPPanelHandler DeleteRequest;

        #region Fields
        FieldContainerType dataProvider;
        List<FieldPanelType> fieldPanels = new List<FieldPanelType>(10);
        bool maximized = true;

        static int defaultFieldTabContainerWidth;
        #endregion


        #region Properties
        public FieldContainerType DataProvider
        {
            get { return dataProvider; }
        }
        #endregion


        #region Public Methods
        /// <summary>
        /// Классы-наследники должны установить все необходимые свойства панели в соотвествии с переданным dataProvider,
        /// если он не null.
        /// </summary>
        public DPPanel(FieldContainerType dataProvider = null)
        {
            //Баг .Net - если не создать Handle, то не будет работать insert табов
            IntPtr a = fieldTabs.Handle;
            
            defaultFieldTabContainerWidth = fieldTabs.Width;

            this.dataProvider = (dataProvider == null) ? new FieldContainerType() : dataProvider;

            sensorTitleText.Text = Preferences.DPPanels.REQUEST_FOR_SENSOR_TITLE;
            sensorTitleText.BackColor = Preferences.UIGlobal.INVALID_INDICATING_COLOR;

            sensorTitleText.GotFocus += new EventHandler(SensorTitleText_GotFocus);
            tabEditBox.GotFocus += new EventHandler(SensorTitleText_GotFocus);
            this.dataProvider.TitleValidChanged += new DataProviderBase.DPHandler(sensor_TitleValidChanged);
            this.dataProvider.ValidChanged += new DataProviderBase.DPHandler(dataProvider_ValidChanged);
            sensorTitleText.TextChanged += new EventHandler(SensorTitleText_TextChanged);
            tabEditBox.TextChanged += new EventHandler(tabEditBox_TextChanged);
            inUseCheck.CheckedChanged += new EventHandler(inUseCheck_CheckedChanged);

            addFieldBtn.Click += new EventHandler(InsertField);
            deleteBtn.Click += new EventHandler(deleteBtn_Click);

            fieldTabs.Click += new EventHandler(fieldTabs_Click);
            maximizeBox.Click += new EventHandler(maximizeBox_Click);
            minimizeBox.Click += new EventHandler(minimizeBox_Click);

            fieldTabs.SelectedIndexChanged += new System.EventHandler(this.fieldTabs_SelectedIndexChanged);


            //Установка свойств панели в соотвествии с DataProvider
            //////////////////////////////////////////////////////////////////////////////////////
            if (dataProvider != null)
            {
                if (DataProvider.Title != null && DataProvider.Title.Length > 0)
                {
                    sensorTitleText.Text = DataProvider.Title;
                    sensorTitleText.BackColor = dataProvider.TitleValid ?
                        SystemColors.Window : Preferences.UIGlobal.INVALID_INDICATING_COLOR;
                }

                inUseCheck.Checked = DataProvider.Checked;

                foreach (FieldType field in DataProvider.Fields)
                {
                    //Generic типу нельзя передать параметры в конструктор, поэтому приходится использовать Activator
                    FieldPanelType newFieldPanel = (FieldPanelType)Activator.CreateInstance(typeof(FieldPanelType), field);
                    AddFieldPanel(newFieldPanel);
                }

                maximized = false;
                AdjustTabControlSize();

                dataProvider_ValidChanged(null);


            }
        }
        #endregion


        #region Private Methods

        void SensorTitleText_GotFocus(object sender, EventArgs e)
        {
            if (((TextBox)sender).Text == Preferences.DPPanels.REQUEST_FOR_SENSOR_TITLE)
            {
                ((TextBox)sender).Text = "";
                ((TextBox)sender).BackColor = Preferences.UIGlobal.INVALID_INDICATING_COLOR;
            }
        }

        void SensorTitleText_TextChanged(object sender, EventArgs e)
        {
            dataProvider.Title = sensorTitleText.Text == Preferences.DPPanels.REQUEST_FOR_SENSOR_TITLE ?
                "" : sensorTitleText.Text;
        }

        void deleteBtn_Click(object sender, EventArgs e)
        {
            if (DeleteRequest != null)
            {
                Leave -= new EventHandler(AdjustTabControlSize);
                DeleteRequest(this);
            }
        }

        void InsertField(object sender, EventArgs e)
        {
            int index = fieldTabs.TabCount > 0 ? fieldTabs.SelectedIndex + 1 : 0;

            FieldPanelType newFieldPanel = new FieldPanelType(); 
            dataProvider.InsertField(index, newFieldPanel.Field);
            InsertFieldPanel(newFieldPanel, index);
        }

        void InsertFieldPanel(FieldPanelType newFieldPanel, int index)
        {
            fieldPanels.Insert(index, newFieldPanel);
            TabPage newTab = new TabPage((newFieldPanel.Field.Title != null && newFieldPanel.Field.Title.Length > 0) ?
                newFieldPanel.Field.Title : Preferences.DPPanels.REQUEST_FOR_SENSOR_TITLE);
            fieldTabs.TabPages.Insert(index, newTab);
            newTab.ImageIndex = newFieldPanel.Field.TitleValid ? -1 : 0;
            newTab.Controls.Add(newFieldPanel);
            fieldTabs.SelectTab(newTab);
            if (fieldTabs.TabCount == 1)
                fieldTabs_SelectedIndexChanged();

            newFieldPanel.DeleteRequest += new DPFieldPanel<FieldType, FieldValueGeneratorBase>.DPFieldPanelHandler(DeleteField);
            newFieldPanel.Field.TitleValidChanged += new DataProviderBase.DPHandler(Field_TitleValidChanged);
            newFieldPanel.SizeChanged += new EventHandler(AdjustTabControlSize);
            AdjustTabControlSize();
        }

        void AddFieldPanel(FieldPanelType newFieldPanel)
        {
            InsertFieldPanel(newFieldPanel, fieldPanels.Count);
        }

        void DeleteField(object sender)
        {
            int delIndex = fieldPanels.IndexOf((FieldPanelType)sender);
            fieldTabs.TabPages.RemoveAt(delIndex);
            fieldPanels.RemoveAt(delIndex);
            dataProvider.RemoveFieldAt(delIndex);
            AdjustTabControlSize();
        }

        protected virtual void AdjustTabControlSize(object sender = null, EventArgs e = null)
        {
            if (fieldTabs.TabCount == 0)
            {
                fieldTabs.Height = 0;
                maximizeBox.Visible = false;
                minimizeBox.Visible = false;
            }
            else
            {
                fieldTabs.Height = fieldTabs.ItemSize.Height*fieldTabs.RowCount +
                    (maximized ? fieldPanels[fieldTabs.SelectedIndex].Height + fieldTabs.Padding.Y : 0);
                
                fieldTabs.Width = Math.Max(defaultFieldTabContainerWidth,
                    fieldPanels[fieldTabs.SelectedIndex].Width + fieldTabs.Padding.X * 2);

                AdjustTabEditBoxToTab(fieldTabs.SelectedIndex);
                tabEditBox.Visible = maximized && ContainsFocus;

                maximizeBox.Visible = !maximized;
                minimizeBox.Visible = maximized;
            }
        }

        void minimizeBox_Click(object sender, EventArgs e)
        {
            maximized = false;
            AdjustTabControlSize();
        }

        void maximizeBox_Click(object sender, EventArgs e)
        {
            maximized = true;
            AdjustTabControlSize();
        }

        void fieldTabs_Click(object sender, EventArgs e)
        {
            maximized = true;
            AdjustTabControlSize();
        }

        void fieldTabs_SelectedIndexChanged(object sender = null, EventArgs e = null)
        {
            if (fieldTabs.TabCount > 0)
            {
                maximized = true;
            }
            else
                tabEditBox.Visible = false;
            AdjustTabControlSize();
        }

        void AdjustTabEditBoxToTab(int index)
        {
            Rectangle rect = fieldTabs.GetTabRect(fieldTabs.SelectedIndex);
            tabEditBox.Location = new Point(rect.X + fieldTabs.Left, rect.Y + fieldTabs.Top);
            tabEditBox.Width = rect.Width;
            tabEditBox.Height = rect.Height;

            tabEditBox.TextChanged -= new EventHandler(tabEditBox_TextChanged);
            tabEditBox.Text = fieldTabs.SelectedTab.Text;
            tabEditBox.TextChanged += new EventHandler(tabEditBox_TextChanged);

            tabEditBox.BackColor = dataProvider.Fields[fieldTabs.SelectedIndex].TitleValid ?
                SystemColors.Window : Preferences.UIGlobal.INVALID_INDICATING_COLOR;
            tabEditBox.Visible = true;
        }

        void sensor_TitleValidChanged(DataProviderBase sender)
        {
            sensorTitleText.BackColor = dataProvider.TitleValid ?
                SystemColors.Window : Preferences.UIGlobal.INVALID_INDICATING_COLOR;
        }

        void Field_TitleValidChanged(DataProviderBase sender)
        {
            int index = dataProvider.Fields.IndexOf((FieldType)sender);
            fieldTabs.TabPages[index].ImageIndex = sender.TitleValid ? -1 : 0;
            if (fieldTabs.SelectedIndex == index)
                tabEditBox.BackColor = sender.TitleValid ? SystemColors.Window : Preferences.UIGlobal.INVALID_INDICATING_COLOR;
            Rectangle rect = fieldTabs.GetTabRect(fieldTabs.SelectedIndex);
            tabEditBox.Location = new Point(rect.X + fieldTabs.Left, rect.Y + fieldTabs.Top);
            tabEditBox.Width = rect.Width;
        }

        void tabEditBox_TextChanged(object sender, EventArgs e)
        {
            fieldTabs.SelectedTab.Text = tabEditBox.Text;
            dataProvider.Fields[fieldTabs.SelectedIndex].Title =
                tabEditBox.Text == Preferences.DPPanels.REQUEST_FOR_SENSOR_TITLE ? "" : tabEditBox.Text; ;
            tabEditBox.Width = fieldTabs.GetTabRect(fieldTabs.SelectedIndex).Width;
        }

        void dataProvider_ValidChanged(DataProviderBase sender)
        {
            errIndicator.Visible = !dataProvider.Valid;
        }

        void inUseCheck_CheckedChanged(object sender, EventArgs e)
        {
            DataProvider.Checked = inUseCheck.Checked;
        }
        #endregion
    }
}
