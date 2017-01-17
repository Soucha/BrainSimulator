﻿using Aga.Controls.Tree;
using GoodAI.BrainSimulator.Forms;
using GoodAI.Core.Observers;
using GoodAI.Core.Utils;
using GoodAI.Modules.School.Common;
using GoodAI.Modules.School.Worlds;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GoodAI.School.Common;
using WeifenLuo.WinFormsUI.Docking;
using YAXLib;

namespace GoodAI.School.GUI
{
    public partial class SchoolRunForm : DockContent
    {
        public List<LearningTaskNode> Data;
        public List<LevelNode> Levels;
        public List<List<AttributeNode>> Attributes;
        public List<List<int>> AttributesChange;
        public PlanDesign Design;

        private const string DEFAULT_FORM_NAME = "School for AI";
        private readonly MainForm m_mainForm;
        private List<DataGridView> LevelGrids;
        private ObserverForm m_observer;

        private int m_currentRow = -1;
        private int m_stepOffset = 0;
        private Stopwatch m_currentLtStopwatch;

        private string m_autosaveFilePath;
        private YAXSerializer m_serializer;
        private TreeModel m_model;
        private string m_lastOpenedFile;
        private string m_uploadedRepresentation;
        private string m_savedRepresentation;
        private string m_currentFile;

        public event EventHandler WorkspaceChanged = delegate { };

        public LearningTaskSelectionForm AddTaskView { get; private set; }
        private bool m_showObserver { get { return btnObserver.Checked; } }

        private bool m_emulateSuccess
        {
            set
            {
                if (m_school != null)
                    m_school.EmulatedUnitSuccessProbability = value ? 1f : 0f;
            }
        }

        private SchoolWorld m_school
        {
            get
            {
                return m_mainForm.Project.World as SchoolWorld;
            }
        }

        private LearningTaskNode CurrentTask
        {
            get
            {
                if (m_currentRow < 0 || m_currentRow >= Data.Count)
                    return null;
                return Data.ElementAt(m_currentRow);
            }
        }

        private IEnumerable<CurriculumNode> ActiveCurricula
        {
            get
            {
                return m_model.Nodes.Where(x => x is CurriculumNode).Select(x => x as CurriculumNode).Where(x => x.IsChecked == true);
            }
        }

        private string CurrentProjectName
        {
            get
            {
                return ActiveCurricula.Count() == 1 ? ActiveCurricula.First().Text : Path.GetFileNameWithoutExtension(m_currentFile);
            }
        }

        private LearningTaskNode SelectedLearningTask
        {
            get
            {
                int dataIndex;
                if (dataGridViewLearningTasks.SelectedRows != null && dataGridViewLearningTasks.SelectedRows.Count > 0)
                {
                    DataGridViewRow row = dataGridViewLearningTasks.SelectedRows[0];
                    dataIndex = row.Index;
                }
                else
                {
                    dataIndex = 0;
                }

                if (Data.Count > dataIndex)
                    return Data[dataIndex];

                return null;
            }
        }

        private PlanDesign m_design
        {
            get
            {
                return CurriculumNode.ToPlanDesign(m_model.Nodes.OfType<CurriculumNode>().ToList());
            }
        }

        //public SchoolRunForm RunView { get; private set; }

        private bool IsProjectUploaded
        {
            get
            {
                if (m_uploadedRepresentation == null)
                    return false;
                string currentRepresentation = m_serializer.Serialize(m_design);
                return m_uploadedRepresentation.Equals(currentRepresentation);
            }
        }

        private bool IsWorkspaceSaved
        {
            get
            {
                if (m_savedRepresentation == null)
                    return false;
                string currentRepresentation = m_serializer.Serialize(m_design);
                return m_savedRepresentation.Equals(currentRepresentation);
            }
        }

        public SchoolRunForm(MainForm mainForm)
        {
            // school main form //

            m_serializer = new YAXSerializer(typeof(PlanDesign));
            m_mainForm = mainForm;
            //RunView = new SchoolRunForm(m_mainForm);

            InitializeComponent();

            m_model = new TreeModel();
            treeViewLTList.Model = m_model;
            treeViewLTList.Refresh();

            if (!String.IsNullOrEmpty(Properties.School.Default.AutosaveFolder))
                btnAutosave.Checked = Properties.School.Default.AutosaveEnabled;
            else
                btnAutosave.Checked = false;

            m_lastOpenedFile = Properties.School.Default.LastOpenedFile;
            LoadCurriculum(m_lastOpenedFile);

            // school run form //

            // here so it does not interfere with designer generated code
            btnRun.Click += m_mainForm.runToolButton_Click;
            btnStop.Click += m_mainForm.stopToolButton_Click;
            btnPause.Click += m_mainForm.pauseToolButton_Click;
            btnStepOver.Click += m_mainForm.stepOverToolButton_Click;
            btnDebug.Click += m_mainForm.debugToolButton_Click;

            m_mainForm.SimulationHandler.StateChanged += SimulationHandler_StateChanged;
            m_mainForm.SimulationHandler.ProgressChanged += SimulationHandler_ProgressChanged;
            m_mainForm.WorldChanged += m_mainForm_WorldChanged;

            nodeTextBox1.DrawText += nodeTextBox1_DrawText;

            m_model.NodesChanged += ModelChanged;
            m_model.NodesInserted += ModelChanged;
            m_model.NodesRemoved += ModelChanged;

            WorkspaceChanged += SchoolRunForm_WorkspaceChanged;

            SchoolRunForm_WorkspaceChanged(null, EventArgs.Empty);
        }

        public void Ready()
        {
            UpdateGridData();
            PrepareSimulation(null, EventArgs.Empty);
            SetObserver();
        }

        public void UpdateGridData()
        {
            dataGridViewLearningTasks.DataSource = Data;
            dataGridViewLearningTasks.Invalidate();
        }

        private void SchoolRunForm_WorkspaceChanged(object sender, EventArgs e)
        {
            UpdateData();
            UpdateWindowName(null, EventArgs.Empty);
            UpdateButtons();
        }

        private void UpdateData()
        {
            if (!Visible || IsProjectUploaded)
                return;

            // update SchoolWorld
            SelectSchoolWorld(null, EventArgs.Empty);
            (m_mainForm.Project.World as SchoolWorld).Curriculum = m_design.AsSchoolCurriculum(m_mainForm.Project.World as SchoolWorld);

            // update curriculum detail grid
            List<LearningTaskNode> data = new List<LearningTaskNode>();
            IEnumerable<LearningTaskNode> ltNodes = ActiveCurricula.
                SelectMany(x => (x as CurriculumNode).Nodes).
                Select(x => x as LearningTaskNode).
                Where(x => x.IsChecked == true);

            foreach (LearningTaskNode ltNode in ltNodes)
                data.Add(ltNode);
            Data = data;
            Design = m_design;
            Ready();
        }

        // thanks to this, form will be emitter of event - not the underlying model (which could be confusing for subscribers)
        private void ModelChanged(object sender, EventArgs e)
        {
            WorkspaceChanged(this, EventArgs.Empty);
        }

        private string GetAutosaveFilename()
        {
            return CurrentProjectName + DateTime.Now.ToString("yyyy-MM-ddTHHmmss"); // ISO 8601
        }

        private void AddWorldHandlers(SchoolWorld world)
        {
            if (world == null)
                return;
            world.CurriculumStarting += PrepareSimulation;
            world.LearningTaskNew += GoToNextTask;
            world.LearningTaskNewLevel += UpdateLTLevel;
            world.LearningTaskFinished += LearningTaskFinished;
            world.VisualFormatChanged += VisualFormatChanged;
            world.TrainingUnitUpdated += UpdateTUStatus;
            world.TrainingUnitFinished += UpdateTUStatus;
            world.TrainingUnitFinished += UpdateTrainingUnitNumber;
        }

        private void RemoveWorldHandlers(SchoolWorld world)
        {
            if (world == null)
                return;
            world.CurriculumStarting -= PrepareSimulation;
            world.LearningTaskNew -= GoToNextTask;
            world.LearningTaskNewLevel -= UpdateLTLevel;
            world.LearningTaskFinished -= LearningTaskFinished;
            world.VisualFormatChanged -= VisualFormatChanged;
            world.TrainingUnitUpdated -= UpdateTUStatus;
            world.TrainingUnitFinished -= UpdateTUStatus;
            world.TrainingUnitFinished -= UpdateTrainingUnitNumber;
        }

        private void UpdateWorldHandlers(SchoolWorld oldWorld, SchoolWorld newWorld)
        {
            if (!Visible)
                return;
            if (newWorld == null)
                Hide();
            if (oldWorld != null)
                RemoveWorldHandlers(oldWorld as SchoolWorld);
            if (newWorld != null)
                AddWorldHandlers(newWorld as SchoolWorld);

            SetObserver();
        }

        private void UpdateButtons()
        {
            btnRun.Enabled = m_mainForm.runToolButton.Enabled;
            btnPause.Enabled = m_mainForm.pauseToolButton.Enabled;
            btnStop.Enabled = m_mainForm.stopToolButton.Enabled;
            btnStepOver.Enabled = m_mainForm.stepOverToolButton.Enabled;
            btnDebug.Enabled = m_mainForm.debugToolButton.Enabled;

            EnableButtons(this);
            EnableToolstripButtons(toolStrip2);

            if (!treeViewLTList.AllNodes.Any())
                btnSave.Enabled = btnSaveAs.Enabled = btnRun.Enabled = false;

            if (treeViewLTList.SelectedNode == null)
            {
                btnNewTask.Enabled = btnDetails.Enabled = false;
                return;
            }

            Node selected = treeViewLTList.SelectedNode.Tag as Node;
            Debug.Assert(selected != null);
        }

        private void UpdateTaskData(ILearningTask runningTask)
        {
            if (CurrentTask == null || runningTask == null)
                return;
            CurrentTask.Steps = (int)m_mainForm.SimulationHandler.SimulationStep - m_stepOffset;
            CurrentTask.Progress = (int)runningTask.Progress;
            TimeSpan diff = m_currentLtStopwatch.Elapsed;
            CurrentTask.Time = (float)Math.Round(diff.TotalSeconds, 2);
            CurrentTask.Status = m_school.TaskResult;

            UpdateGridData();
        }

        private void SetObserver()
        {
            if (m_showObserver && m_school != null)
            {
                if (m_observer == null)
                {
                    try
                    {
                        MyMemoryBlockObserver observer = new MyMemoryBlockObserver();
                        observer.Target = m_school.VisualFOV;

                        m_observer = new ObserverForm(m_mainForm, observer, m_school);

                        m_observer.TopLevel = false;
                        dockPanelObserver.Controls.Add(m_observer);

                        m_observer.CloseButtonVisible = false;
                        m_observer.MaximizeBox = false;
                        m_observer.Size = dockPanelObserver.Size + new System.Drawing.Size(16, 38);
                        m_observer.Location = new System.Drawing.Point(-8, -30);

                        m_observer.Show();
                    }
                    catch (Exception e)
                    {
                        MyLog.ERROR.WriteLine("Error creating observer: " + e.Message);
                    }
                }
                else
                {
                    m_observer.Show();
                    dockPanelObserver.Show();

                    m_observer.Observer.GenericTarget = m_school.VisualFOV;
                }
            }
            else
            {
                if (m_observer != null)
                {
                    //(m_observer.Observer as MyMemoryBlockObserver).Target = null;
                    m_observer.Close();
                    m_observer.Dispose();
                    m_observer = null;
                    dockPanelObserver.Hide();
                }
            }
        }

        private void HighlightCurrentTask()
        {
            DataGridViewCellStyle defaultStyle = new DataGridViewCellStyle();
            DataGridViewCellStyle highlightStyle = new DataGridViewCellStyle();
            highlightStyle.BackColor = Color.PaleGreen;

            var focus = GetFocusedControl();

            dataGridViewLearningTasks.Rows[m_currentRow].Selected = true;

            if (focus != null)
            {
                focus.Focus();
            }

            foreach (DataGridViewRow row in dataGridViewLearningTasks.Rows)
                foreach (DataGridViewCell cell in row.Cells)
                    if (row.Index == m_currentRow)
                        cell.Style = highlightStyle;
                    else
                        cell.Style = defaultStyle;
        }

        #region UI

        private void ApplyToAll(Control parent, Action<Control> apply)
        {
            foreach (Control control in parent.Controls)
            {
                if (control.HasChildren)
                    ApplyToAll(control, apply);
                apply(control);
            }
        }

        private void SetButtonsEnabled(Control control, bool value)
        {
            Action<Control> setBtns = (x) =>
            {
                Button b = x as Button;
                if (b != null)
                    b.Enabled = value;
            };
            ApplyToAll(control, setBtns);
        }

        private void SetToolstripButtonsEnabled(Control control, bool value)
        {
            ToolStrip tools = control as ToolStrip;
            if (tools != null)
                foreach (ToolStripItem item in tools.Items)
                    if (item as ToolStripButton != null)
                        item.Enabled = value;
        }

        private void DisableButtons(Control control)
        {
            SetButtonsEnabled(control, false);
            SetToolstripButtonsEnabled(control, false);
        }

        private void EnableButtons(Control control)
        {
            SetButtonsEnabled(control, true);
        }

        private void EnableToolstripButtons(ToolStrip toolstrip)
        {
            SetToolstripButtonsEnabled(toolstrip, true);
        }

        #endregion UI

        private bool AddFileContent(bool clearWorkspace = false)
        {
            if (openFileDialog1.ShowDialog() != DialogResult.OK)
                return false;
            if (clearWorkspace)
                m_model.Nodes.Clear();
            LoadCurriculum(openFileDialog1.FileName);
            return true;
        }

        #region (De)serialization

        private void SaveProject(string path)
        {
            string xmlResult;
            CurriculumManager.SavePlanDesign(m_design, path, out xmlResult);

            m_savedRepresentation = xmlResult;
            m_currentFile = path;
            UpdateWindowName(null, EventArgs.Empty);
            UpdateData();
        }

        private void LoadCurriculum(string filePath)
        {
            string xmlCurr;
            PlanDesign plan = CurriculumManager.LoadPlanDesign(filePath, out xmlCurr);
            if (plan == null) return;

            foreach (CurriculumNode curr in CurriculumNode.FromPlanDesign(plan))
                m_model.Nodes.Add(curr);

            Properties.School.Default.LastOpenedFile = filePath;
            Properties.School.Default.Save();
            m_savedRepresentation = xmlCurr;
            m_currentFile = filePath;
        }

        #endregion (De)serialization

        private void disableLearningTaskPanel()
        {
            splitContainer3.Panel1.Enabled = false;
        }

        private void enableLearningTaskPanel()
        {
            splitContainer3.Panel1.Enabled = true;
        }

        private void ExportDataGridViewData(string filename, TextDataFormat format = TextDataFormat.CommaSeparatedValue)
        {
            IDataObject objectSave = Clipboard.GetDataObject();
            bool multiSelectAllowed = dataGridViewLearningTasks.MultiSelect;
            DataGridViewClipboardCopyMode copyMode = dataGridViewLearningTasks.ClipboardCopyMode;

            dataGridViewLearningTasks.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            dataGridViewLearningTasks.MultiSelect = true;
            dataGridViewLearningTasks.SelectAll();
            Clipboard.SetDataObject(dataGridViewLearningTasks.GetClipboardContent());
            if (format == TextDataFormat.CommaSeparatedValue && Path.GetExtension(filename) != ".csv")
                filename += ".csv";

            File.WriteAllText(filename, Clipboard.GetText(format));

            dataGridViewLearningTasks.MultiSelect = multiSelectAllowed;
            dataGridViewLearningTasks.ClipboardCopyMode = copyMode;
            if (objectSave != null)
                Clipboard.SetDataObject(objectSave);
        }
    }
}
