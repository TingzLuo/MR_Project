# MR Project - 基于 SCG 的 MR 推荐系统

一个基于 SCG（结构化内容图）的 MR（机器阅读理解）推荐系统。系统通过解析文档生成 SCG 结构化内容图，再基于 SCG 进行 MR 推荐，实现从文档到阅读理解的智能化流程。

## 技术栈

### 前端
- **Vue 3** + **Vite** — 构建工具
- **Element Plus** — UI 组件库
- **Pinia** — 状态管理
- **Vue Router** — 路由管理
- **ECharts** — 数据可视化图表
- **LogicFlow** — 流程图/结构化图展示
- **Axios** — HTTP 请求

### 后端
- **.NET 8** (ASP.NET Core Web API)
- **Entity Framework Core** + **Pomelo MySQL** — ORM 与数据库
- **JWT Bearer** — 身份认证
- **PdfPig** — PDF 文档解析
- **OpenXml** — Office 文档处理
- **Swagger** — API 文档

## 项目结构

```
MR_Project/
├── frontend/                # 前端项目
│   ├── src/
│   │   ├── api/             # 接口请求
│   │   ├── components/      # 公共组件
│   │   ├── layout/          # 布局组件
│   │   ├── router/          # 路由配置
│   │   ├── stores/          # Pinia 状态管理
│   │   ├── utils/           # 工具函数
│   │   └── views/           # 页面视图
│   │       ├── auth/        # 登录/注册
│   │       ├── user/        # 用户端页面
│   │       └── admin/       # 管理端页面
│   ├── index.html
│   ├── package.json
│   └── vite.config.js
│
└── backend/                 # 后端项目
    └── MRProject.Api/
        ├── Controllers/     # API 控制器
        ├── Services/        # 业务逻辑层
        ├── Entities/        # 数据库实体
        ├── DTOs/            # 数据传输对象
        ├── Data/            # 数据库上下文
        ├── Common/          # 公共类/配置
        ├── Enums/           # 枚举定义
        └── Middleware/      # 中间件
```

## 核心流程

```
文档上传 → 文档解析 → SCG 生成 → MR 推荐 → 阅读理解结果
```

1. **文档上传与解析**：用户上传 PDF/Word 文档，系统自动解析文档内容
2. **SCG 生成**：基于文档内容，通过 LLM 生成结构化内容图（Structure Content Graph），以节点和边的形式表达文档的语义结构
3. **MR 推荐**：基于生成的 SCG，系统智能推荐相关的机器阅读理解任务
4. **阅读理解**：用户可对推荐的 MR 任务进行查看、编辑和确认

## 功能模块

### 用户端
| 模块 | 说明 |
|------|------|
| 仪表盘 | 用户数据统计、操作趋势图表 |
| 文档管理 | 上传、查看、删除文档，支持 PDF/Word 等格式 |
| SCG 生成 | 基于文档内容生成结构化内容图，支持图可视化展示与编辑 |
| MR 推荐 | 基于 SCG 智能推荐机器阅读理解任务，支持结果查看与保存 |
| 操作记录 | 查看历史操作活动 |
| 个人中心 | 修改个人信息、修改密码 |

### 管理端
| 模块 | 说明 |
|------|------|
| 管理仪表盘 | 系统概览、用户统计 |
| 用户管理 | 用户增删改查、批量操作、状态管理 |
| 数据备份导出 | 系统数据备份与导出 |

## 快速开始

### 环境要求
- Node.js >= 18
- .NET 8 SDK
- MySQL 8.x

### 前端启动

```bash
cd frontend
npm install
npm run dev
```

前端默认运行在 `http://localhost:5173`

### 后端启动

```bash
cd backend/MRProject.Api
dotnet restore
dotnet run
```

后端默认运行在 `https://localhost:7xxx`，Swagger 文档地址：`/swagger`

### 数据库配置

在 `backend/MRProject.Api/appsettings.json` 中配置 MySQL 连接字符串：

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Port=3306;Database=mr_project;User=root;Password=your_password;"
  }
}
```

## API 接口

| 模块 | 路由前缀 | 说明 |
|------|----------|------|
| 认证 | `/api/auth` | 登录、注册、获取当前用户 |
| 文档 | `/api/documents` | 文档上传、查询、删除 |
| SCG | `/api/scg` | SCG 结构化内容图生成与管理 |
| MR | `/api/mr` | 基于 SCG 的 MR 推荐与阅读理解 |
| 仪表盘 | `/api/dashboard` | 数据统计 |
| 用户 | `/api/users` | 个人信息管理 |
| 管理 | `/api/admin` | 管理员功能 |

## 许可证

本项目仅供学习与研究使用。
