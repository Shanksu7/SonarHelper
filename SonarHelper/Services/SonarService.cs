using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;
using RestSharp;
using SonarHelper.Models;
using System.Linq;

namespace SonarHelper.Services
{
    public class SonarService
    {
        private const string DataFileName = "sonarprojects.json";
        private const string CommandsFileName = "sonar-commands.json";
        private List<SonarProject> _projects;
        private SonarCommandsConfig _commands;

        public SonarService()
        {
            _projects = LoadProjects();
            _commands = LoadCommands();
        }

        private List<SonarProject> LoadProjects()
        {
            if (!File.Exists(DataFileName))
                return new List<SonarProject>();

            var json = File.ReadAllText(DataFileName);
            return JsonConvert.DeserializeObject<List<SonarProject>>(json) ?? new List<SonarProject>();
        }

        private SonarCommandsConfig LoadCommands()
        {
            if (!File.Exists(CommandsFileName))
            {
                var defaultConfig = new SonarCommandsConfig
                {
                    Commands = new List<SonarCommand>
                    {
                        new SonarCommand
                        {
                            Name = "Análisis Básico",
                            Command = "dotnet sonarscanner begin /k:\"{name}\" /d:sonar.host.url=\"http://localhost:9000\" /d:sonar.token=\"{token}\" && dotnet build && dotnet sonarscanner end /d:sonar.token=\"{token}\"",
                            Required = true
                        },
                        new SonarCommand
                        {
                            Name = "Análisis con Cobertura",
                            Command = "dotnet sonarscanner begin /k:\"{name}\" /d:sonar.token=\"{token}\" /d:sonar.cs.vscoveragexml.reportsPaths=coverage.xml /d:sonar.host.url=\"http://localhost:9000\" && dotnet build --no-incremental && dotnet-coverage collect \"dotnet test\" -f xml -o \"coverage.xml\" && dotnet sonarscanner end /d:sonar.token=\"{token}\"",
                            Required = true
                        }
                    }
                };

                SaveCommands(defaultConfig);
                return defaultConfig;
            }

            var json = File.ReadAllText(CommandsFileName);
            return JsonConvert.DeserializeObject<SonarCommandsConfig>(json) ?? new SonarCommandsConfig();
        }

        private void SaveCommands(SonarCommandsConfig config)
        {
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(CommandsFileName, json);
        }

        private void SaveProjects()
        {
            var json = JsonConvert.SerializeObject(_projects, Formatting.Indented);
            File.WriteAllText(DataFileName, json);
        }

        public List<SonarProject> GetAllProjects()
        {
            return _projects;
        }

        public List<SonarCommand> GetCommands()
        {
            return _commands.Commands;
        }

        public (bool success, string message) ValidateProjectPath(string projectPath)
        {
            if (!Directory.Exists(projectPath))
                return (false, "La ruta especificada no existe.");

            // Buscar archivos .sln en el directorio y sus subdirectorios
            var slnFiles = Directory.GetFiles(projectPath, "*.sln", SearchOption.TopDirectoryOnly);
            
            if (slnFiles.Length == 0)
                return (false, "No se encontró ningún archivo .sln en la ruta especificada o sus subdirectorios.");

            if (slnFiles.Length > 1)
                return (false, "Se encontraron múltiples archivos .sln. Por favor, especifique la ruta que contenga solo una solución.");

            return (true, "Validación exitosa");
        }

        public async Task<(bool success, string message, string output)> ExecuteCommand(SonarCommand command, SonarProject project)
        {
            try
            {
                // Desglosar el comando si contiene &&
                var subCommands = command.Command.Split(new[] { "&&" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(cmd => cmd.Trim())
                    .ToList();

                var allOutput = new System.Text.StringBuilder();
                var allErrors = new System.Text.StringBuilder();

                for (int i = 0; i < subCommands.Count; i++)
                {
                    var subCommand = subCommands[i];
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        WorkingDirectory = project.ProjectPath,
                        Arguments = $"/c {subCommand.Replace("{name}", project.ProjectName).Replace("{token}", project.SonarToken)}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using var process = new Process { StartInfo = processStartInfo };
                    process.Start();

                    var outputBuilder = new System.Text.StringBuilder();
                    var errorBuilder = new System.Text.StringBuilder();

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            outputBuilder.AppendLine(e.Data);
                            allOutput.AppendLine(e.Data);
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            errorBuilder.AppendLine(e.Data);
                            allErrors.AppendLine(e.Data);
                        }
                    };

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        var errorMessage = $"Error al ejecutar parte {i + 1} de {subCommands.Count} del comando '{command.Name}'. Código de salida: {process.ExitCode}";
                        if (!string.IsNullOrEmpty(errorBuilder.ToString()))
                        {
                            errorMessage += $"\nErrores:\n{errorBuilder}";
                        }
                        return (false, errorMessage, allOutput.ToString());
                    }
                }

                return (true, $"Comando '{command.Name}' ejecutado exitosamente.", allOutput.ToString());
            }
            catch (Exception ex)
            {
                return (false, $"Error al ejecutar '{command.Name}': {ex.Message}", string.Empty);
            }
        }

        public (bool success, string message) AddProject(SonarProject project)
        {
            if (_projects.Contains(project))
                return (false, "Ya existe un proyecto con el mismo nombre, token o ruta.");

            var validation = ValidateProjectPath(project.ProjectPath);
            if (!validation.success)
                return validation;

            _projects.Add(project);
            SaveProjects();
            return (true, "Proyecto agregado exitosamente");
        }

        public bool UpdateProject(SonarProject project)
        {
            var index = _projects.FindIndex(p => p.ProjectName == project.ProjectName);
            if (index == -1)
                return false;

            var validation = ValidateProjectPath(project.ProjectPath);
            if (!validation.success)
                return false;

            _projects[index] = project;
            SaveProjects();
            return true;
        }

        public bool DeleteProject(string projectName)
        {
            var project = _projects.Find(p => p.ProjectName == projectName);
            if (project == null)
                return false;

            _projects.Remove(project);
            SaveProjects();
            return true;
        }

        public async Task<bool> IsSonarQubeRunning()
        {
            try
            {
                var client = new RestClient("http://localhost:9000");
                var request = new RestRequest("/api/system/status");
                var response = await client.ExecuteGetAsync(request);
                return response.IsSuccessful;
            }
            catch
            {
                return false;
            }
        }

        public void UpdateLastAnalysisDate(string projectName)
        {
            var project = _projects.Find(p => p.ProjectName == projectName);
            if (project != null)
            {
                project.LastAnalysisDate = DateTime.Now;
                SaveProjects();
            }
        }
    }
} 