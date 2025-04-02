using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using SonarHelper.Models;
using SonarHelper.Services;
using System.Linq;

namespace SonarHelper
{
    public partial class Form1 : Form
    {
        private readonly SonarService _sonarService;
        private SonarProject? _selectedProject;

        public Form1()
        {
            InitializeComponent();
            _sonarService = new SonarService();
            InitializeCustomComponents();
            LoadProjects();
        }

        private void InitializeCustomComponents()
        {
            // Lista de proyectos
            listProjects = new ListBox
            {
                Dock = DockStyle.Left,
                Width = 200
            };
            listProjects.SelectedIndexChanged += ListProjects_SelectedIndexChanged;

            // Panel de detalles
            Panel detailsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // Campos de texto
            txtProjectName = new TextBox { Width = 350 };
            txtSonarToken = new TextBox { Width = 350 };
            txtProjectPath = new TextBox { Width = 350 };

            // Botones
            btnBrowse = new Button { Text = "Examinar", Width = 80 };
            btnAdd = new Button { Text = "Agregar", Width = 80 };
            btnUpdate = new Button { Text = "Actualizar", Width = 80 };
            btnDelete = new Button { Text = "Eliminar", Width = 80 };
            btnRunAnalysis = new Button { Text = "Ejecutar Análisis", Width = 120 };

            btnBrowse.Click += BtnBrowse_Click;
            btnAdd.Click += BtnAdd_Click;
            btnUpdate.Click += BtnUpdate_Click;
            btnDelete.Click += BtnDelete_Click;
            btnRunAnalysis.Click += BtnRunAnalysis_Click;

            // Área de log
            txtLog = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Dock = DockStyle.Bottom,
                Height = 150
            };

            // Layout
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(10)
            };

            layout.Controls.Add(new Label { Text = "Nombre del Proyecto:", Width = 360 }, 0, 0);
            layout.Controls.Add(txtProjectName, 0, 1);
            layout.Controls.Add(new Label { Text = "Token de Sonar:", Width = 360 }, 0, 2);
            layout.Controls.Add(txtSonarToken, 0, 3);

            FlowLayoutPanel pathPanel = new FlowLayoutPanel() { Width = 360 };
            pathPanel.Controls.Add(new Label { Text = "Ruta del Proyecto:", Width = 360 });
            pathPanel.Controls.Add(txtProjectPath);
            pathPanel.Controls.Add(btnBrowse);
            layout.Controls.Add(pathPanel, 0, 5);

            FlowLayoutPanel buttonsPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Dock = DockStyle.Fill
            };
            buttonsPanel.Controls.AddRange(new Control[] { btnAdd, btnUpdate, btnDelete, btnRunAnalysis });
            layout.Controls.Add(buttonsPanel, 0, 6);

            detailsPanel.Controls.Add(layout);

            // Agregar controles al formulario
            Controls.Add(detailsPanel);
            Controls.Add(listProjects);
            Controls.Add(txtLog);

            // Configurar el formulario
            Text = "SonarHelper";
            MinimumSize = new Size(600, 600);
        }

        private void LoadProjects()
        {
            listProjects.Items.Clear();
            foreach (var project in _sonarService.GetAllProjects())
            {
                listProjects.Items.Add(project.ProjectName);
            }
        }

        private void ListProjects_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listProjects.SelectedItem == null)
                return;

            string projectName = listProjects.SelectedItem.ToString();
            _selectedProject = _sonarService.GetAllProjects().Find(p => p.ProjectName == projectName);

            if (_selectedProject != null)
            {
                txtProjectName.Text = _selectedProject.ProjectName;
                txtSonarToken.Text = _selectedProject.SonarToken;
                txtProjectPath.Text = _selectedProject.ProjectPath;
            }
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtProjectPath.Text = dialog.SelectedPath;
                }
            }
        }

        private void LogMessage(string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => LogMessage(message)));
                return;
            }

            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            txtLog.ScrollToCaret();
        }

        private async void BtnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtProjectName.Text) ||
                string.IsNullOrWhiteSpace(txtSonarToken.Text) ||
                string.IsNullOrWhiteSpace(txtProjectPath.Text))
            {
                MessageBox.Show("Por favor complete todos los campos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            LogMessage($"Intentando agregar proyecto: {txtProjectName.Text}");
            var project = new SonarProject
            {
                ProjectName = txtProjectName.Text,
                SonarToken = txtSonarToken.Text,
                ProjectPath = txtProjectPath.Text
            };

            var result = _sonarService.AddProject(project);
            if (result.success)
            {
                LogMessage($"Proyecto agregado exitosamente: {project.ProjectName}");
                LoadProjects();
                ClearFields();
                MessageBox.Show(result.message, "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                LogMessage($"Error al agregar proyecto: {result.message}");
                MessageBox.Show(result.message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (_selectedProject == null)
            {
                MessageBox.Show("Por favor seleccione un proyecto para actualizar.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            LogMessage($"Actualizando proyecto: {_selectedProject.ProjectName}");
            var updatedProject = new SonarProject
            {
                ProjectName = txtProjectName.Text,
                SonarToken = txtSonarToken.Text,
                ProjectPath = txtProjectPath.Text,
                LastAnalysisDate = _selectedProject.LastAnalysisDate
            };

            if (_sonarService.UpdateProject(updatedProject))
            {
                LogMessage($"Proyecto actualizado exitosamente: {updatedProject.ProjectName}");
                LoadProjects();
                MessageBox.Show("Proyecto actualizado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                LogMessage($"Error al actualizar proyecto: {updatedProject.ProjectName}");
                MessageBox.Show("Error al actualizar el proyecto.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedProject == null)
            {
                MessageBox.Show("Por favor seleccione un proyecto para eliminar.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (MessageBox.Show("¿Está seguro de que desea eliminar este proyecto?", "Confirmar eliminación",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                LogMessage($"Eliminando proyecto: {_selectedProject.ProjectName}");
                if (_sonarService.DeleteProject(_selectedProject.ProjectName))
                {
                    LogMessage($"Proyecto eliminado exitosamente: {_selectedProject.ProjectName}");
                    LoadProjects();
                    ClearFields();
                    MessageBox.Show("Proyecto eliminado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    LogMessage($"Error al eliminar proyecto: {_selectedProject.ProjectName}");
                    MessageBox.Show("Error al eliminar el proyecto.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void BtnRunAnalysis_Click(object sender, EventArgs e)
        {
            if (_selectedProject == null)
            {
                MessageBox.Show("Por favor seleccione un proyecto para ejecutar el análisis.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Deshabilitar controles durante la ejecución
            btnRunAnalysis.Enabled = false;
            btnAdd.Enabled = false;
            btnUpdate.Enabled = false;
            btnDelete.Enabled = false;
            listProjects.Enabled = false;

            try
            {
                LogMessage($"Iniciando análisis para el proyecto: {_selectedProject.ProjectName}");
                LogMessage($"Ruta del proyecto: {_selectedProject.ProjectPath}");
                
                if (!await _sonarService.IsSonarQubeRunning())
                {
                    LogMessage("Error: SonarQube no está ejecutándose");
                    MessageBox.Show("SonarQube no está ejecutándose. Por favor inicie SonarQube antes de continuar.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                LogMessage("Verificación de SonarQube exitosa");

                var commands = _sonarService.GetCommands();
                LogMessage($"Se encontraron {commands.Count} comandos para ejecutar");

                for (int i = 0; i < commands.Count; i++)
                {
                    var command = commands[i];
                    LogMessage($"\n=== Ejecutando comando {i + 1} de {commands.Count}: {command.Name} ===");
                    
                    // Desglosar el comando para mostrar sus partes
                    var subCommands = command.Command.Split(new[] { "&&" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(cmd => cmd.Trim())
                        .ToList();
                    
                    LogMessage($"El comando se desglosa en {subCommands.Count} partes:");
                    for (int j = 0; j < subCommands.Count; j++)
                    {
                        LogMessage($"Parte {j + 1}: {subCommands[j]}");
                    }
                    
                    var result = await _sonarService.ExecuteCommand(command, _selectedProject);
                    
                    if (!string.IsNullOrEmpty(result.output))
                    {
                        LogMessage("\nSalida del comando:");
                        foreach (var line in result.output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            LogMessage(line);
                        }
                    }
                    
                    if (!result.success)
                    {
                        LogMessage($"\nError en comando '{command.Name}': {result.message}");
                        if (command.Required)
                        {
                            MessageBox.Show(result.message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            break;
                        }
                        else
                        {
                            var continuar = MessageBox.Show(
                                $"{result.message}\n\n¿Desea continuar con los siguientes comandos?",
                                "Error en comando no requerido",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning
                            );

                            if (continuar == DialogResult.No)
                                break;
                        }
                    }
                    else
                    {
                        LogMessage($"\nComando '{command.Name}' ejecutado exitosamente");
                    }
                }

                _sonarService.UpdateLastAnalysisDate(_selectedProject.ProjectName);
                LogMessage("\n=== Análisis completado exitosamente ===");
                MessageBox.Show("Todos los análisis se completaron exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"\nError inesperado: {ex.Message}");
                if (ex.StackTrace != null)
                {
                    LogMessage("\nStack Trace:");
                    foreach (var line in ex.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        LogMessage(line);
                    }
                }
                MessageBox.Show($"Error al ejecutar el análisis: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Rehabilitar controles
                btnRunAnalysis.Enabled = true;
                btnAdd.Enabled = true;
                btnUpdate.Enabled = true;
                btnDelete.Enabled = true;
                listProjects.Enabled = true;
            }
        }

        private void ClearFields()
        {
            txtProjectName.Text = string.Empty;
            txtSonarToken.Text = string.Empty;
            txtProjectPath.Text = string.Empty;
            _selectedProject = null;
        }

        private ListBox listProjects;
        private TextBox txtProjectName;
        private TextBox txtSonarToken;
        private TextBox txtProjectPath;
        private TextBox txtLog;
        private Button btnBrowse;
        private Button btnAdd;
        private Button btnUpdate;
        private Button btnDelete;
        private Button btnRunAnalysis;
    }
}
