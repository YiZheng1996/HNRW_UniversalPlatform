using MainUI.UniversalPlatform.Core.Application.Interfaces;
using MainUI.UniversalPlatform.Core.Domain.Variables;
using MainUI.UniversalPlatform.Core.Domain.Workflows;

namespace MainUI.UniversalPlatform.UI.WorkflowDesigner.Controls
{
    #region 工具箱控件

    /// <summary>
    /// 工具箱控件 - 显示可用的步骤类型
    /// </summary>
    public class ToolboxControl : UserControl
    {
        private TreeView _treeView;
        private TextBox _searchBox;
        private List<StepTypeInfo> _allStepTypes = new();

        public event Action<string> ToolSelected;
        public event Action<string, DragEventArgs> ToolDragStart;

        public ToolboxControl()
        {
            InitializeControls();
        }

        private void InitializeControls()
        {
            // 搜索框
            _searchBox = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("微软雅黑", 9)
            };
            _searchBox.TextChanged += (s, e) => FilterTools(_searchBox.Text);

            // 树形视图
            _treeView = new TreeView
            {
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 9),
                ItemHeight = 26,
                ShowLines = true,
                ShowPlusMinus = true,
                ShowRootLines = true,
                AllowDrop = false
            };
            _treeView.NodeMouseDoubleClick += OnNodeDoubleClick;
            _treeView.ItemDrag += OnItemDrag;

            // 添加标题
            var titleLabel = new Label
            {
                Text = "📦 工具箱",
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("微软雅黑", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 0, 0),
                BackColor = Color.FromArgb(240, 240, 240)
            };

            Controls.Add(_treeView);
            Controls.Add(_searchBox);
            Controls.Add(titleLabel);
        }

        /// <summary>
        /// 设置步骤类型列表
        /// </summary>
        public void SetStepTypes(IEnumerable<StepTypeInfo> stepTypes)
        {
            _allStepTypes = stepTypes?.ToList() ?? new List<StepTypeInfo>();
            RefreshTreeView();
        }

        /// <summary>
        /// 刷新树形视图
        /// </summary>
        private void RefreshTreeView()
        {
            _treeView.Nodes.Clear();

            // 按类别分组
            var grouped = _allStepTypes.GroupBy(s => s.Category ?? "其他");

            foreach (var group in grouped)
            {
                var categoryNode = new TreeNode(GetCategoryDisplayName(group.Key))
                {
                    Tag = "Category",
                    NodeFont = new Font("微软雅黑", 9, FontStyle.Bold)
                };

                foreach (var stepType in group)
                {
                    var stepNode = new TreeNode($"{stepType.IconKey} {stepType.DisplayName}")
                    {
                        Tag = stepType.Name,
                        ToolTipText = stepType.Description
                    };
                    categoryNode.Nodes.Add(stepNode);
                }

                _treeView.Nodes.Add(categoryNode);
            }

            _treeView.ExpandAll();
        }

        /// <summary>
        /// 筛选工具
        /// </summary>
        private void FilterTools(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                RefreshTreeView();
                return;
            }

            _treeView.Nodes.Clear();

            var filtered = _allStepTypes
                .Where(s => s.DisplayName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                           (s.Description?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

            var searchResultNode = new TreeNode($"搜索结果 ({filtered.Count})");
            foreach (var stepType in filtered)
            {
                var stepNode = new TreeNode($"{stepType.IconKey} {stepType.DisplayName}")
                {
                    Tag = stepType.Name
                };
                searchResultNode.Nodes.Add(stepNode);
            }

            _treeView.Nodes.Add(searchResultNode);
            searchResultNode.Expand();
        }

        private void OnNodeDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag is string stepName && stepName != "Category")
            {
                ToolSelected?.Invoke(stepName);
            }
        }

        private void OnItemDrag(object sender, ItemDragEventArgs e)
        {
            if (e.Item is TreeNode node && node.Tag is string stepName && stepName != "Category")
            {
                DoDragDrop(stepName, DragDropEffects.Copy);
            }
        }

        private string GetCategoryDisplayName(string category)
        {
            return category switch
            {
                "Logic" => "🔧 逻辑控制",
                "Condition" => "❓ 条件判断",
                "Loop" => "🔄 循环控制",
                "Variable" => "📊 变量操作",
                "Communication" => "📡 通信操作",
                "Report" => "📝 报表操作",
                "Monitor" => "👁 监控操作",
                _ => $"📁 {category}"
            };
        }
    }

    #endregion

    #region 步骤列表控件

    /// <summary>
    /// 步骤列表控件 - 显示和管理工作流步骤
    /// </summary>
    public class StepGridControl : UserControl
    {
        private DataGridView _dataGridView;
        private List<WorkflowStep> _steps = new();
        private int _highlightedIndex = -1;

        public event Action<int> StepSelected;
        public event Action<int> StepDoubleClicked;
        public event Action<int, int> StepMoved;
        public event Action<int> StepDeleted;
        public event Action<string, int?> DragDropped;

        public int SelectedIndex
        {
            get => _dataGridView.CurrentRow?.Index ?? -1;
            set
            {
                if (value >= 0 && value < _dataGridView.Rows.Count)
                {
                    _dataGridView.ClearSelection();
                    _dataGridView.Rows[value].Selected = true;
                    _dataGridView.CurrentCell = _dataGridView.Rows[value].Cells[0];
                }
            }
        }

        public bool AllowEdit { get; set; } = true;

        public StepGridControl()
        {
            InitializeControls();
        }

        private void InitializeControls()
        {
            _dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AllowDrop = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersHeight = 35,
                RowTemplate = { Height = 32 },
                Font = new Font("微软雅黑", 9)
            };

            // 定义列
            _dataGridView.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn
                {
                    Name = "StepNumber",
                    HeaderText = "序号",
                    Width = 50,
                    FillWeight = 10
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "StepName",
                    HeaderText = "步骤名称",
                    Width = 150,
                    FillWeight = 30
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "Status",
                    HeaderText = "状态",
                    Width = 80,
                    FillWeight = 15
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "Remark",
                    HeaderText = "备注",
                    Width = 200,
                    FillWeight = 45
                }
            });

            // 事件
            _dataGridView.SelectionChanged += (s, e) =>
            {
                if (_dataGridView.CurrentRow != null)
                    StepSelected?.Invoke(_dataGridView.CurrentRow.Index);
            };

            _dataGridView.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                    StepDoubleClicked?.Invoke(e.RowIndex);
            };

            _dataGridView.KeyDown += OnKeyDown;
            _dataGridView.DragEnter += OnDragEnter;
            _dataGridView.DragDrop += OnDragDrop;
            _dataGridView.CellFormatting += OnCellFormatting;

            // 右键菜单
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("配置", null, (s, e) =>
            {
                if (SelectedIndex >= 0) StepDoubleClicked?.Invoke(SelectedIndex);
            });
            contextMenu.Items.Add("上移", null, (s, e) =>
            {
                if (SelectedIndex > 0) StepMoved?.Invoke(SelectedIndex, SelectedIndex - 1);
            });
            contextMenu.Items.Add("下移", null, (s, e) =>
            {
                if (SelectedIndex >= 0 && SelectedIndex < _steps.Count - 1)
                    StepMoved?.Invoke(SelectedIndex, SelectedIndex + 1);
            });
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("删除", null, (s, e) =>
            {
                if (SelectedIndex >= 0) StepDeleted?.Invoke(SelectedIndex);
            });
            _dataGridView.ContextMenuStrip = contextMenu;

            Controls.Add(_dataGridView);
        }

        /// <summary>
        /// 设置步骤列表
        /// </summary>
        public void SetSteps(IReadOnlyList<WorkflowStep> steps)
        {
            _steps = steps?.ToList() ?? new List<WorkflowStep>();
            RefreshGrid();
        }

        /// <summary>
        /// 刷新表格
        /// </summary>
        public void RefreshGrid()
        {
            _dataGridView.Rows.Clear();

            foreach (var step in _steps)
            {
                var row = new DataGridViewRow();
                row.CreateCells(_dataGridView,
                    step.StepNumber,
                    step.StepName,
                    GetStatusText(step.Status),
                    step.Remark ?? ""
                );
                row.Tag = step;
                _dataGridView.Rows.Add(row);
            }
        }

        /// <summary>
        /// 更新步骤状态
        /// </summary>
        public void UpdateStepStatus(int index, StepStatus status, string errorMessage)
        {
            if (index >= 0 && index < _dataGridView.Rows.Count)
            {
                _dataGridView.Rows[index].Cells["Status"].Value = GetStatusText(status);
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    _dataGridView.Rows[index].Cells["Remark"].Value = errorMessage;
                }
            }
        }

        /// <summary>
        /// 滚动到指定步骤
        /// </summary>
        public void ScrollToStep(int index)
        {
            if (index >= 0 && index < _dataGridView.Rows.Count)
            {
                _dataGridView.FirstDisplayedScrollingRowIndex = Math.Max(0, index - 3);
            }
        }

        /// <summary>
        /// 高亮步骤
        /// </summary>
        public void HighlightStep(int index)
        {
            _highlightedIndex = index;
            _dataGridView.Invalidate();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!AllowEdit) return;

            if (e.KeyCode == Keys.Delete && SelectedIndex >= 0)
            {
                StepDeleted?.Invoke(SelectedIndex);
                e.Handled = true;
            }
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(string)))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(string)))
            {
                var stepName = (string)e.Data.GetData(typeof(string));
                var clientPoint = _dataGridView.PointToClient(new Point(e.X, e.Y));
                var hitTest = _dataGridView.HitTest(clientPoint.X, clientPoint.Y);
                int? insertIndex = hitTest.RowIndex >= 0 ? hitTest.RowIndex : null;

                DragDropped?.Invoke(stepName, insertIndex);
            }
        }

        private void OnCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex == _highlightedIndex)
            {
                e.CellStyle.BackColor = Color.FromArgb(255, 243, 205);
            }

            // 状态列着色
            if (_dataGridView.Columns[e.ColumnIndex].Name == "Status")
            {
                var value = e.Value?.ToString();
                e.CellStyle.ForeColor = value switch
                {
                    "✓ 成功" => Color.Green,
                    "✗ 失败" => Color.Red,
                    "▶ 执行中" => Color.Orange,
                    _ => Color.Gray
                };
            }
        }

        private string GetStatusText(StepStatus status)
        {
            return status switch
            {
                StepStatus.Pending => "⏳ 待执行",
                StepStatus.Running => "▶ 执行中",
                StepStatus.Succeeded => "✓ 成功",
                StepStatus.Failed => "✗ 失败",
                StepStatus.Skipped => "⏭ 跳过",
                _ => "未知"
            };
        }
    }

    #endregion

    #region 变量面板控件

    /// <summary>
    /// 变量面板控件 - 显示和管理工作流变量
    /// </summary>
    public class VariablePanelControl : UserControl
    {
        private DataGridView _dataGridView;
        private Button _btnAdd;
        private Button _btnDelete;

        public event Action AddVariableClicked;
        public event Action<string> DeleteVariableClicked;

        public VariablePanelControl()
        {
            InitializeControls();
        }

        private void InitializeControls()
        {
            // 工具栏
            var toolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35
            };

            _btnAdd = new Button
            {
                Text = "➕ 添加",
                Location = new Point(5, 5),
                Size = new Size(70, 25),
                FlatStyle = FlatStyle.Flat
            };
            _btnAdd.Click += (s, e) => AddVariableClicked?.Invoke();

            _btnDelete = new Button
            {
                Text = "➖ 删除",
                Location = new Point(80, 5),
                Size = new Size(70, 25),
                FlatStyle = FlatStyle.Flat
            };
            _btnDelete.Click += (s, e) =>
            {
                if (_dataGridView.CurrentRow?.Tag is Variable v)
                    DeleteVariableClicked?.Invoke(v.Name);
            };

            toolbar.Controls.Add(_btnAdd);
            toolbar.Controls.Add(_btnDelete);

            // 数据表格
            _dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("微软雅黑", 9)
            };

            _dataGridView.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "变量名", FillWeight = 30 },
                new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "类型", FillWeight = 20 },
                new DataGridViewTextBoxColumn { Name = "Value", HeaderText = "当前值", FillWeight = 30 },
                new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "说明", FillWeight = 20 }
            });

            Controls.Add(_dataGridView);
            Controls.Add(toolbar);
        }

        /// <summary>
        /// 设置变量列表
        /// </summary>
        public void SetVariables(IEnumerable<Variable> variables)
        {
            _dataGridView.Rows.Clear();

            foreach (var v in variables ?? Enumerable.Empty<Variable>())
            {
                var row = new DataGridViewRow();
                row.CreateCells(_dataGridView,
                    v.Name,
                    v.Type.ToTypeString(),
                    v.GetStringValue(),
                    v.DisplayText
                );
                row.Tag = v;

                // 系统变量用灰色显示
                if (v.IsSystem)
                {
                    row.DefaultCellStyle.ForeColor = Color.Gray;
                }

                _dataGridView.Rows.Add(row);
            }
        }
    }

    #endregion
}
