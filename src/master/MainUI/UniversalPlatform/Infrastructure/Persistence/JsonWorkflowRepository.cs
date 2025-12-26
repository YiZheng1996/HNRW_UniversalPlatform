using MainUI.UniversalPlatform.Core.Abstractions;
using MainUI.UniversalPlatform.Core.Domain.Variables;
using MainUI.UniversalPlatform.Core.Domain.Workflows;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MainUI.UniversalPlatform.Infrastructure.Persistence
{
    /// <summary>
    /// JSON工作流仓储实现
    /// 将工作流配置持久化到JSON文件
    /// </summary>
    public class JsonWorkflowRepository : IWorkflowRepository
    {
        private readonly string _basePath;
        private readonly ILogger<JsonWorkflowRepository> _logger;
        private readonly JsonSerializerSettings _jsonSettings;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public JsonWorkflowRepository(
            ILogger<JsonWorkflowRepository> logger,
            string basePath = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _basePath = basePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Procedure");

            _jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatString = "yyyy-MM-dd HH:mm:ss"
            };
        }

        /// <summary>
        /// 获取工作流文件路径
        /// </summary>
        public string GetFilePath(string modelType, string modelName, string itemName)
        {
            return Path.Combine(_basePath, modelType, modelName, $"{itemName}.json");
        }

        /// <summary>
        /// 加载工作流
        /// </summary>
        public async Task<Workflow> LoadAsync(
            string modelType,
            string modelName,
            string itemName,
            CancellationToken cancellationToken = default)
        {
            var filePath = GetFilePath(modelType, modelName, itemName);

            await _lock.WaitAsync(cancellationToken);
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogInformation("工作流文件不存在，创建新工作流: {Path}", filePath);
                    return new Workflow(modelType, modelName, itemName);
                }

                var json = await File.ReadAllTextAsync(filePath, cancellationToken);
                var dto = JsonConvert.DeserializeObject<WorkflowFileDto>(json, _jsonSettings);

                if (dto == null)
                {
                    _logger.LogWarning("工作流文件格式无效: {Path}", filePath);
                    return new Workflow(modelType, modelName, itemName);
                }

                // 转换为领域模型
                var workflow = ConvertToDomain(dto, modelType, modelName, itemName);

                _logger.LogDebug("成功加载工作流: {Id}, 步骤数: {StepCount}",
                    workflow.Id, workflow.StepCount);

                return workflow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载工作流失败: {Path}", filePath);
                throw;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// 保存工作流
        /// </summary>
        public async Task SaveAsync(Workflow workflow, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(workflow);

            var filePath = GetFilePath(workflow.ModelType, workflow.ModelName, workflow.ItemName);

            await _lock.WaitAsync(cancellationToken);
            try
            {
                // 确保目录存在
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 转换为DTO
                var dto = ConvertToDto(workflow);

                // 序列化并保存
                var json = JsonConvert.SerializeObject(dto, _jsonSettings);
                await File.WriteAllTextAsync(filePath, json, cancellationToken);

                _logger.LogInformation("工作流已保存: {Path}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存工作流失败: {Path}", filePath);
                throw;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// 检查工作流是否存在
        /// </summary>
        public Task<bool> ExistsAsync(
            string modelType,
            string modelName,
            string itemName,
            CancellationToken cancellationToken = default)
        {
            var filePath = GetFilePath(modelType, modelName, itemName);
            return Task.FromResult(File.Exists(filePath));
        }

        /// <summary>
        /// 删除工作流
        /// </summary>
        public async Task DeleteAsync(
            string modelType,
            string modelName,
            string itemName,
            CancellationToken cancellationToken = default)
        {
            var filePath = GetFilePath(modelType, modelName, itemName);

            await _lock.WaitAsync(cancellationToken);
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("工作流已删除: {Path}", filePath);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// 获取指定产品类型和型号下的所有工作流
        /// </summary>
        public Task<IEnumerable<WorkflowSummary>> GetWorkflowsAsync(
            string modelType,
            string modelName,
            CancellationToken cancellationToken = default)
        {
            var directory = Path.Combine(_basePath, modelType, modelName);
            var summaries = new List<WorkflowSummary>();

            if (Directory.Exists(directory))
            {
                var files = Directory.GetFiles(directory, "*.json");
                foreach (var file in files)
                {
                    var itemName = Path.GetFileNameWithoutExtension(file);
                    var fileInfo = new FileInfo(file);

                    // 简单读取步骤数量
                    int stepCount = 0;
                    try
                    {
                        var json = File.ReadAllText(file);
                        var dto = JsonConvert.DeserializeObject<WorkflowFileDto>(json);
                        stepCount = dto?.Form?.FirstOrDefault()?.ChildSteps?.Count ?? 0;
                    }
                    catch { }

                    summaries.Add(new WorkflowSummary
                    {
                        ModelType = modelType,
                        ModelName = modelName,
                        ItemName = itemName,
                        StepCount = stepCount,
                        LastModified = fileInfo.LastWriteTime,
                        FilePath = file
                    });
                }
            }

            return Task.FromResult<IEnumerable<WorkflowSummary>>(summaries);
        }

        #region 转换方法

        /// <summary>
        /// DTO转领域模型
        /// </summary>
        private Workflow ConvertToDomain(WorkflowFileDto dto, string modelType, string modelName, string itemName)
        {
            var formData = dto.Form?.FirstOrDefault();
            if (formData == null)
            {
                return new Workflow(modelType, modelName, itemName);
            }

            var steps = formData.ChildSteps?.Select((s, index) =>
                WorkflowStep.Reconstitute(
                    id: Guid.NewGuid(),
                    stepNumber: s.StepNum > 0 ? s.StepNum : index + 1,
                    stepName: s.StepName ?? "未知步骤",
                    parameter: s.StepParameter,
                    remark: s.Remark,
                    status: (StepStatus)s.Status,
                    errorMessage: s.ErrorMessage
                )
            ).ToList() ?? new List<WorkflowStep>();

            var createdAt = DateTime.TryParse(dto.System?.CreateTime, out var ct) ? ct : DateTime.Now;

            return Workflow.Reconstitute(
                modelType: modelType,
                modelName: modelName,
                itemName: itemName,
                steps: steps,
                createdAt: createdAt,
                modifiedAt: DateTime.Now
            );
        }

        /// <summary>
        /// 领域模型转DTO
        /// </summary>
        private WorkflowFileDto ConvertToDto(Workflow workflow)
        {
            return new WorkflowFileDto
            {
                System = new SystemInfoDto
                {
                    CreateTime = workflow.CreatedAt.ToString("yyyy/MM/dd HH:mm:ss"),
                    ProjectName = "软件通用平台"
                },
                Form =
                [
                    new() {
                        ModelTypeName = workflow.ModelType,
                        ModelName = workflow.ModelName,
                        ItemName = workflow.ItemName,
                        ChildSteps = workflow.Steps.Select(s => new StepDto
                        {
                            StepNum = s.StepNumber,
                            StepName = s.StepName,
                            StepParameter = s.Parameter,
                            Remark = s.Remark,
                            Status = (int)s.Status,
                            ErrorMessage = s.ErrorMessage
                        }).ToList()
                    }
                ],
                Variable = []
            };
        }

        Task<Workflow> IWorkflowRepository.LoadAsync(string modelType, string modelName, string itemName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region DTO类（兼容现有JSON格式）

        private class WorkflowFileDto
        {
            public SystemInfoDto System { get; set; }
            public List<FormDataDto> Form { get; set; }
            public List<VariableDto> Variable { get; set; }
        }

        private class SystemInfoDto
        {
            public string CreateTime { get; set; }
            public string ProjectName { get; set; }
        }

        private class FormDataDto
        {
            public string ModelTypeName { get; set; }
            public string ModelName { get; set; }
            public string ItemName { get; set; }
            public List<StepDto> ChildSteps { get; set; }
        }

        private class StepDto
        {
            public int StepNum { get; set; }
            public string StepName { get; set; }
            public object StepParameter { get; set; }
            public string Remark { get; set; }
            public int Status { get; set; }
            public string ErrorMessage { get; set; }
        }

        private class VariableDto
        {
            public string VarName { get; set; }
            public string VarType { get; set; }
            public object VarValue { get; set; }
            public string VarText { get; set; }
        }

        #endregion
    }

    /// <summary>
    /// JSON变量仓储实现
    /// </summary>
    public class JsonVariableRepository(
        ILogger<JsonVariableRepository> logger,
        string basePath = null) : IVariableRepository
    {
        private readonly ILogger<JsonVariableRepository> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly string _basePath = basePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Procedure");
        private readonly SemaphoreSlim _lock = new(1, 1);

        private string GetFilePath(string modelType, string modelName, string itemName)
        {
            return Path.Combine(_basePath, modelType, modelName, $"{itemName}.json");
        }

        public async Task<IEnumerable<Variable>> LoadAsync(
            string modelType,
            string modelName,
            string itemName,
            CancellationToken cancellationToken = default)
        {
            var filePath = GetFilePath(modelType, modelName, itemName);

            await _lock.WaitAsync(cancellationToken);
            try
            {
                if (!File.Exists(filePath))
                    return Enumerable.Empty<Variable>();

                var json = await File.ReadAllTextAsync(filePath, cancellationToken);
                var dto = JsonConvert.DeserializeObject<dynamic>(json);

                var variables = new List<Variable>();
                var varArray = dto?.Variable as IEnumerable<dynamic>;

                if (varArray != null)
                {
                    foreach (var v in varArray)
                    {
                        string name = v.VarName?.ToString();
                        string typeStr = v.VarType?.ToString() ?? "string";
                        object value = v.VarValue;
                        string text = v.VarText?.ToString();

                        if (!string.IsNullOrEmpty(name))
                        {
                            var varType = VariableTypeExtensions.ParseVariableType(typeStr);
                            var variable = Variable.Reconstitute(
                                name: name,
                                type: varType,
                                value: value,
                                displayText: text,
                                scope: VariableScope.Workflow,
                                isSystem: false,
                                lastUpdated: DateTime.Now
                            );
                            variables.Add(variable);
                        }
                    }
                }

                _logger.LogDebug("加载了 {Count} 个变量", variables.Count);
                return variables;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载变量失败: {Path}", filePath);
                return Enumerable.Empty<Variable>();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task SaveAsync(
            string modelType,
            string modelName,
            string itemName,
            IEnumerable<Variable> variables,
            CancellationToken cancellationToken = default)
        {
            var filePath = GetFilePath(modelType, modelName, itemName);

            await _lock.WaitAsync(cancellationToken);
            try
            {
                // 读取现有文件
                dynamic dto;
                if (File.Exists(filePath))
                {
                    var json = await File.ReadAllTextAsync(filePath, cancellationToken);
                    dto = JsonConvert.DeserializeObject<dynamic>(json);
                }
                else
                {
                    dto = new { System = new { CreateTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") }, Form = new List<object>(), Variable = new List<object>() };
                }

                // 更新变量部分
                var varList = variables.Select(v => new
                {
                    VarName = v.Name,
                    VarType = v.Type.ToTypeString(),
                    VarValue = v.Value,
                    VarText = v.DisplayText
                }).ToList();

                // 重新构建完整对象并保存
                var newDto = new
                {
                    dto.System,
                    dto.Form,
                    Variable = varList
                };

                var newJson = JsonConvert.SerializeObject(newDto, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, newJson, cancellationToken);

                _logger.LogInformation("保存了 {Count} 个变量", varList.Count);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task AddAsync(
            string modelType,
            string modelName,
            string itemName,
            Variable variable,
            CancellationToken cancellationToken = default)
        {
            var existing = (await LoadAsync(modelType, modelName, itemName, cancellationToken)).ToList();
            existing.Add(variable);
            await SaveAsync(modelType, modelName, itemName, existing, cancellationToken);
        }

        public async Task DeleteAsync(
            string modelType,
            string modelName,
            string itemName,
            string variableName,
            CancellationToken cancellationToken = default)
        {
            var existing = (await LoadAsync(modelType, modelName, itemName, cancellationToken)).ToList();
            existing.RemoveAll(v => v.Name == variableName);
            await SaveAsync(modelType, modelName, itemName, existing, cancellationToken);
        }
    }
}
