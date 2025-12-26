using MainUI.LogicalConfiguration.LogicalManager;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MainUI.LogicalConfiguration.Services
{
    /// <summary>
    /// 工作流配置服务实现 - 封装所有JSON操作
    /// 
    /// 职责：
    /// - 管理配置文件的读写
    /// - 提供线程安全的文件访问
    /// - 触发配置变更事件
    /// 
    /// 生命周期：Scoped（每个窗体实例一个）
    /// </summary>
    /// <remarks>
    /// 构造函数
    /// </remarks>
    public class WorkflowConfigurationService(ILogger<WorkflowConfigurationService> logger) : IWorkflowConfigurationService, IDisposable
    {
        #region 私有字段

        private readonly ILogger<WorkflowConfigurationService> _logger = logger ?? 
            throw new ArgumentNullException(nameof(logger));
        private readonly SemaphoreSlim _fileLock = new(1, 1);

        private string _modelType;
        private string _modelName;
        private string _processName;
        private string _configPath;
        private bool _disposed;

        #endregion

        #region 属性

        /// <summary>
        /// 配置文件路径
        /// </summary>
        public string ConfigurationPath => _configPath;

        #endregion

        #region 事件

        /// <summary>
        /// 配置变更事件
        /// </summary>
        public event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;

        #endregion

        #region 初始化方法

        /// <summary>
        /// 初始化配置路径
        /// </summary>
        public void Initialize(string modelType, string modelName, string processName)
        {
            _modelType = modelType ?? throw new ArgumentNullException(nameof(modelType));
            _modelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
            _processName = processName ?? throw new ArgumentNullException(nameof(processName));

            var modelPath = Path.Combine(Application.StartupPath, "Procedure", modelType, modelName);
            _configPath = Path.Combine(modelPath, $"{processName}.json");

            _logger.LogInformation("配置服务初始化: {Path}", _configPath);
        }

        /// <summary>
        /// 确保配置文件存在
        /// </summary>
        public async Task EnsureConfigurationExistsAsync(string modelType, string modelName, string processName)
        {
            Initialize(modelType, modelName, processName);

            var directory = Path.GetDirectoryName(_configPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("创建配置目录: {Directory}", directory);
            }

            if (!File.Exists(_configPath))
            {
                var defaultConfig = BuildDefaultJsonConfig();
                await SaveJsonConfigAsync(defaultConfig);
                _logger.LogInformation("创建默认配置文件: {Path}", _configPath);

                OnConfigurationChanged(ConfigurationChangeType.Created, defaultConfig);
            }
        }

        #endregion

        #region 变量操作

        /// <summary>
        /// 加载变量配置
        /// </summary>
        public async Task<List<VarItem>> LoadVariablesAsync()
        {
            await _fileLock.WaitAsync();
            try
            {
                var config = await LoadJsonConfigAsync();
                return config?.Variable ?? [];
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <summary>
        /// 保存变量配置
        /// </summary>
        public async Task SaveVariablesAsync(IEnumerable<VarItem> variables)
        {
            await _fileLock.WaitAsync();
            try
            {
                var config = await LoadJsonConfigAsync() ?? BuildDefaultJsonConfig();
                config.Variable = variables.ToList();
                await SaveJsonConfigAsync(config);

                _logger.LogDebug("保存变量配置，共 {Count} 个变量", config.Variable.Count);
                OnConfigurationChanged(ConfigurationChangeType.VariablesUpdated, variables);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        #endregion

        #region 步骤操作

        /// <summary>
        /// 加载步骤配置
        /// </summary>
        public async Task<List<ChildModel>> LoadStepsAsync()
        {
            await _fileLock.WaitAsync();
            try
            {
                var config = await LoadJsonConfigAsync();
                return config?.Form?.FirstOrDefault()?.ChildSteps ?? [];
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <summary>
        /// 保存步骤配置
        /// </summary>
        public async Task SaveStepsAsync(IEnumerable<ChildModel> steps)
        {
            await _fileLock.WaitAsync();
            try
            {
                var config = await LoadJsonConfigAsync() ?? BuildDefaultJsonConfig();

                if (config.Form == null || config.Form.Count == 0)
                {
                    config.Form = new List<Parent>
                    {
                        new Parent
                        {
                            ModelTypeName = _modelType,
                            ModelName = _modelName,
                            ItemName = _processName,
                            ChildSteps = steps.ToList()
                        }
                    };
                }
                else
                {
                    config.Form[0].ChildSteps = steps.ToList();
                }

                await SaveJsonConfigAsync(config);

                _logger.LogDebug("保存步骤配置，共 {Count} 个步骤", steps.Count());
                OnConfigurationChanged(ConfigurationChangeType.StepsUpdated, steps);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        #endregion

        #region 完整配置操作

        /// <summary>
        /// 加载完整配置
        /// </summary>
        public async Task<WorkflowConfiguration> LoadFullConfigurationAsync()
        {
            await _fileLock.WaitAsync();
            try
            {
                var jsonConfig = await LoadJsonConfigAsync();
                if (jsonConfig == null) return null;

                var parent = jsonConfig.Form?.FirstOrDefault();

                return new WorkflowConfiguration
                {
                    System = new WorkflowConfiguration.SystemInfo
                    {
                        CreateTime = jsonConfig.System?.CreateTime,
                        ProjectName = jsonConfig.System?.ProjectName
                    },
                    ModelType = parent?.ModelTypeName ?? _modelType,
                    ModelName = parent?.ModelName ?? _modelName,
                    ProcessName = parent?.ItemName ?? _processName,
                    Variables = jsonConfig.Variable ?? new List<VarItem>(),
                    Steps = parent?.ChildSteps ?? new List<ChildModel>()
                };
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <summary>
        /// 保存完整配置
        /// </summary>
        public async Task SaveFullConfigurationAsync(WorkflowConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            await _fileLock.WaitAsync();
            try
            {
                var jsonConfig = new JsonManager.JsonConfig
                {
                    System = new JsonManager.JsonConfig.SystemInfo
                    {
                        CreateTime = configuration.System?.CreateTime ?? DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                        ProjectName = configuration.System?.ProjectName ?? "软件通用平台"
                    },
                    Form =
                    [
                        new Parent
                        {
                            ModelTypeName = configuration.ModelType,
                            ModelName = configuration.ModelName,
                            ItemName = configuration.ProcessName,
                            ChildSteps = configuration.Steps ?? new List<ChildModel>()
                        }
                    ],
                    Variable = configuration.Variables ?? new List<VarItem>()
                };

                await SaveJsonConfigAsync(jsonConfig);

                _logger.LogInformation("保存完整配置");
                OnConfigurationChanged(ConfigurationChangeType.FullReload, configuration);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 加载JSON配置
        /// </summary>
        private async Task<JsonManager.JsonConfig> LoadJsonConfigAsync()
        {
            if (!File.Exists(_configPath))
            {
                _logger.LogWarning("配置文件不存在: {Path}", _configPath);
                return null;
            }

            try
            {
                var json = await File.ReadAllTextAsync(_configPath);
                return JsonConvert.DeserializeObject<JsonManager.JsonConfig>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取配置文件失败: {Path}", _configPath);
                throw;
            }
        }

        /// <summary>
        /// 保存JSON配置
        /// </summary>
        private async Task SaveJsonConfigAsync(JsonManager.JsonConfig config)
        {
            try
            {
                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                await File.WriteAllTextAsync(_configPath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存配置文件失败: {Path}", _configPath);
                throw;
            }
        }

        /// <summary>
        /// 构建默认JSON配置
        /// </summary>
        private JsonManager.JsonConfig BuildDefaultJsonConfig()
        {
            return new JsonManager.JsonConfig
            {
                System = new JsonManager.JsonConfig.SystemInfo
                {
                    CreateTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                    ProjectName = "软件通用平台"
                },
                Form =
                [
                    new Parent
                    {
                        ModelTypeName = _modelType,
                        ModelName = _modelName,
                        ItemName = _processName,
                        ChildSteps = []
                    }
                ],
                Variable = []
            };
        }

        /// <summary>
        /// 触发配置变更事件
        /// </summary>
        private void OnConfigurationChanged(ConfigurationChangeType type, object data)
        {
            ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
            {
                ChangeType = type,
                ChangedData = data,
                ChangedAt = DateTime.Now
            });
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _fileLock?.Dispose();
            _disposed = true;

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
