# 11 mini-project độc lập để học từ Document-First

Tài liệu này biến các kỹ thuật trong `document_first` thành 11 project standalone: mỗi project sở hữu database, tables, migrations, API, seed data và tests riêng; không project nào tham chiếu hoặc gọi project khác.

## 1. Kết luận sau khi đọc source

`document_first` không phải một CRUD mẫu. Đây là modular monolith .NET 8 quản lý tài liệu kỹ thuật theo nguyên tắc **database là nguồn sự thật**, rồi render, release, publish, search và phân tích quan hệ từ dữ liệu đó.

Tài liệu cũ `mini-projects.md` chủ động thiết kế theo kiểu tích lũy:

- Mini 1 là kernel cho các mini sau.
- Auth là nền cho quyền truy cập document.
- Release dùng draft CRUD và Markdown generator.
- Publish, search, graph, import/export dùng dữ liệu của các phase trước.

Cách đó phù hợp để dựng lại toàn bộ hệ thống, nhưng không đáp ứng yêu cầu “mỗi project chạy độc lập”. File này dùng quy tắc ngược lại:

> **Học lại cùng một pattern, không dùng lại runtime, database hay source project của bài trước.**

Hai file cũ được giữ nguyên để không làm mất nội dung bạn đã có:

- `document_first.API/Document/mini-projects.md`
- `document_first.API/Document/mini_project_1_kernel.md`

## 2. Project hiện tại giải quyết bài toán gì

Hệ thống cho BA, PO, developer và QA tạo ba loại tài liệu có cấu trúc:

- `UserStory`: story statement, preconditions, flows, acceptance criteria, priority, sprint.
- `Tdd`: goals, architecture, data model, diagrams, API endpoints, examples, error codes.
- `BusinessRule`: statement, điều kiện áp dụng, hành động, ngoại lệ, owner và nguồn tham chiếu.

Luồng chính:

```text
Người dùng nhập form
        |
        v
Draft quan hệ hóa trong PostgreSQL
        |
        +----> Preview Markdown
        +----> Full-text / trigram / vector search
        +----> Dependency graph / impact analysis
        |
        v
Release trong transaction
        |
        v
JSON snapshot + Markdown bất biến
        |
        v
Outbox publish_jobs ----> Quartz worker ----> GitHub
```

### 2.1. Các tầng

| Tầng | Trách nhiệm chính |
|---|---|
| `document_first.API` | Controllers, JWT/cookie, authorization policies, rate limit, middleware, health checks, Quartz jobs, DI. |
| `document_first.Service` | Nghiệp vụ auth, project, document, render, release, publish, search, graph, import/export. |
| `document_first.Repo` | 28 tables, EF Core mappings, PostgreSQL constraints/indexes, migrations và seed data. |
| `document_first.Exporter` | CLI render database ra filesystem để kiểm tra kết quả. |
| `document_first.Tests` | Integration tests với PostgreSQL thật qua Testcontainers, fake external gateways và golden files. |

`WeatherForecastController` là code mẫu còn sót lại của template ASP.NET Core, không thuộc nghiệp vụ chính.

### 2.2. Bốn nhóm dữ liệu

| Nhóm | Tables chính | Ý nghĩa |
|---|---|---|
| Identity và tenancy | `users`, `refresh_tokens`, `user_tokens`, `projects`, `project_members` | Xác thực, phiên đăng nhập, workspace và RBAC. |
| Draft có cấu trúc | `documents` cùng các bảng detail, flow, criterion, diagram, API, tag, assignee, link | Bản hiện tại có thể sửa và query theo từng phần. |
| Release và publish | `document_versions`, `document_activities`, `github_repositories`, `publish_jobs` | Snapshot bất biến, audit và reliable background publishing. |
| Search | `document_chunks` cùng `documents.search_vector` | Full-text bỏ dấu, trigram fallback và semantic search bằng pgvector. |

### 2.3. Các quyết định đáng học

1. **Draft quan hệ hóa, version dùng snapshot JSON.** Draft cần sửa/query từng phần; version cần bất biến và render lại được.
2. **Markdown là output.** Source of truth nằm trong database, không nằm trong file Markdown.
3. **Release là transaction boundary.** Snapshot, version number, lifecycle, activity và publish jobs phải nhất quán.
4. **Outbox tách release khỏi GitHub.** GitHub hỏng không được làm mất version đã release.
5. **Validation nằm ở application và database.** Service trả lỗi dễ hiểu; CHECK/UNIQUE/FK chặn mọi đường ghi khác.
6. **Search có graceful degradation.** Embedding chết thì lexical search vẫn hoạt động.
7. **External systems đi qua interface.** GitHub và embedding có gateway/provider để test bằng fake.
8. **Integration test dùng PostgreSQL thật.** In-memory EF không kiểm tra được generated column, PostgreSQL SQL, pgvector hay constraint thật.

### 2.4. Những phần không nên copy mù quáng

Source hiện tại có các điểm tốt để biến thành bài tập cải tiến:

- Refresh-token rotation chưa khóa/transaction hóa toàn bộ thao tác đọc-token-cũ rồi tạo-token-mới.
- `LoadFullAsync` bị lặp ở nhiều service.
- Import chưa tái sử dụng toàn bộ validator và link resolution của đường ghi thông thường.
- Publish worker dùng `break` khi một repository gần hết rate limit, có thể bỏ qua job đã claim của repository khác trong cùng batch.
- Re-render nhiều document không có cơ chế chống release song song trên từng document.
- Upload ZIP cần giới hạn số entry, tổng dung lượng giải nén và chống path traversal rõ ràng hơn.

Các điểm này được ghi trong `documentation/review.md`; khi làm mini-project, hãy sửa nguyên nhân thay vì chép nguyên trạng.

## 3. Hợp đồng độc lập bắt buộc

Mỗi mini-project phải thỏa toàn bộ điều kiện sau:

- Có solution riêng, ví dụ `Mini07.ReliablePublisher.sln`.
- Có database riêng, ví dụ `mini07_publisher`; không dùng schema chung trong `document_first`.
- Có `docker-compose.yml` riêng và volume riêng.
- Có migrations riêng; xóa database rồi migrate lại vẫn chạy được.
- Có seed data riêng đủ để demo trong Swagger.
- Không có `ProjectReference` đến project khác trong danh sách này.
- Không gọi HTTP, queue hay database của project khác.
- Không tạo `SharedKernel` dùng chung giữa các mini-project.
- Nếu bài không học auth, dùng `X-Demo-User-Id` hoặc một user seed cố định; không gọi Mini 2.
- Nếu bài cần document, tự có table document tối giản; không gọi Mini 4, 5 hoặc 6.
- Test phải tự dựng dependency của chính nó và chạy độc lập.

Cấu trúc tối thiểu nên giữ đơn giản:

```text
mini-xx-name/
├── src/
│   ├── MiniXX.Api/
│   ├── MiniXX.Application/
│   └── MiniXX.Data/
├── tests/
│   └── MiniXX.Tests/
├── docker-compose.yml
├── MiniXX.sln
└── README.md
```

Ba project class library không phải luật cứng. Với bài đầu tiên, một Web API project và một test project là đủ; chỉ tách tầng khi file bắt đầu khó tìm.

## 4. Bản đồ 11 mini-project

Thứ tự dưới đây là **độ khó học**, không phải dependency kỹ thuật.

| Cấp | Mini-project | Trọng tâm | Tables |
|---|---|---|---:|
| 1 | Helpdesk Ticket API | CRUD, filter, paging, constraint, concurrency | 4 |
| 1 | Secure Session API | Password, JWT, refresh rotation, one-time token | 4 |
| 1 | Team Workspace RBAC | Membership, role hierarchy, authorization | 4 |
| 1 | Markdown Knowledge Publisher | Structured data, deterministic renderer, golden test | 4 |
| 2 | User Story Planner | Aggregate, replace-section update, domain validation | 5 |
| 2 | Policy Version Vault | Snapshot, hash, release, diff, restore, audit | 3 |
| 2 | Reliable GitHub Publisher | Outbox, worker lock, retry, idempotency | 4 |
| 3 | Searchable Knowledge Base | FTS, trigram, chunking, vector, RRF | 3 |
| 3 | Dependency Impact Analyzer | Directed graph, cycle, dangling link, impact | 3 |
| 3 | API Contract Registry | OpenAPI parsing, endpoint/error registry, comparison | 5 |
| 3 | Bulk Import/Export Hub | Dry run, batch errors, ZIP safety, idempotency | 4 |

## 5. Cấp 1 — Nền tảng thực chiến

### Mini 1 — Helpdesk Ticket API

**Sản phẩm:** API quản lý ticket hỗ trợ nội bộ cho một công ty nhỏ.

**Tables:**

| Table | Cột chính | Constraint quan trọng |
|---|---|---|
| `tickets` | `id`, `code`, `title`, `description`, `priority`, `status`, `assignee_name`, `row_version`, timestamps | `code` unique; `priority` và `status` có CHECK; title không rỗng. |
| `ticket_comments` | `id`, `ticket_id`, `author_name`, `content`, `created_at` | FK cascade; content không rỗng. |
| `labels` | `id`, `name`, `slug`, `color` | `slug` unique; color đúng dạng hex. |
| `ticket_labels` | `ticket_id`, `label_id` | Composite PK chống gắn trùng. |

**API tối thiểu:**

- `POST /tickets`
- `GET /tickets?status=&priority=&assignee=&q=&page=&pageSize=`
- `GET /tickets/{id}`
- `PUT /tickets/{id}`
- `DELETE /tickets/{id}`
- `POST /tickets/{id}/comments`
- `PUT /tickets/{id}/labels`

**Luật nghiệp vụ:**

- Code sinh dạng `TCK-0001`, tăng nguyên tử trong database.
- Ticket `Closed` không nhận comment mới.
- Update phải kiểm tra optimistic concurrency.
- Query list projection trực tiếp sang response; không load toàn entity graph.
- Paging có giới hạn `pageSize`, sort ổn định theo `created_at` và `id`.

**Tests bắt buộc:** code không trùng khi tạo song song, filter kết hợp đúng, paging không lặp/mất dòng, label không gắn trùng, stale update trả `409`.

**Học từ source:** `DocumentService/Service.cs`, `DocumentKeyGenerator.cs`, `DocumentConfiguration.cs`, `SchemaTests.cs`.

**Không làm:** auth, Markdown, versioning, background worker.

### Mini 2 — Secure Session API

**Sản phẩm:** dịch vụ đăng ký, đăng nhập và quản lý phiên cho một ứng dụng web.

**Tables:**

| Table | Cột chính | Constraint quan trọng |
|---|---|---|
| `users` | `id`, `email`, `password_hash`, `full_name`, `email_verified_at`, `is_active` | Email normalized lowercase và unique. |
| `refresh_tokens` | `id`, `user_id`, `token_hash`, `expires_at`, `revoked_at`, `replaced_by_token_id`, client info | Chỉ lưu hash; `token_hash` unique. |
| `one_time_tokens` | `id`, `user_id`, `purpose`, `token_hash`, `expires_at`, `used_at` | Token dùng một lần, có purpose. |
| `login_attempts` | `id`, `email_hash`, `ip_address`, `succeeded`, `created_at` | Index theo email/IP/time để lockout. |

**API tối thiểu:** register, login, refresh, logout, logout-all, verify-email, forgot-password, reset-password, `GET /me`.

**Luật nghiệp vụ:**

- Password dùng BCrypt; access token ngắn hạn; refresh token ngẫu nhiên mạnh.
- Response login sai giống nhau cho email không tồn tại và password sai.
- Forgot-password luôn trả acknowledgement trung tính.
- Refresh rotation chạy trong transaction và khóa token cũ; hai request song song chỉ một request thành công.
- Dùng lại refresh token đã rotate phải revoke cả token family.
- Reset password làm vô hiệu mọi session còn sống.

**Tests bắt buộc:** duplicate email, neutral errors, refresh rotation, replay detection, expired/used token, account disabled, race hai refresh request.

**Học từ source:** `AuthService/Service.cs`, `TokenHasher.cs`, `JwtService`, `AuthConfigurations.cs`, `AuthTests.cs`.

**Không làm:** workspace, role, project membership. API này hoàn chỉnh dù chỉ có user và session.

### Mini 3 — Team Workspace RBAC

**Sản phẩm:** API quản lý workspace và thành viên cho một SaaS team nhỏ.

**Tables:**

| Table | Cột chính | Constraint quan trọng |
|---|---|---|
| `people` | `id`, `email`, `display_name`, `is_active` | Email unique. |
| `workspaces` | `id`, `code`, `name`, `description`, `created_by_id` | Code unique và chuẩn hóa uppercase. |
| `workspace_members` | `workspace_id`, `person_id`, `role`, `joined_at` | Composite PK; role thuộc `Owner/Admin/Editor/Viewer`. |
| `workspace_invitations` | `id`, `workspace_id`, `email`, `role`, `token_hash`, `expires_at`, `accepted_at` | Một invitation đang hoạt động cho mỗi email/workspace. |

**Cách xác định user:** dùng header `X-Demo-User-Id` và seed `people`; đây là một phần của project này, không phụ thuộc Mini 2.

**API tối thiểu:** CRUD workspace, list/add/change/remove member, create/accept invitation, `GET /workspaces/mine`.

**Luật nghiệp vụ:**

- Role số nhỏ hơn có quyền cao hơn nhưng mọi check phải đi qua `Covers(requiredRole)`.
- Chỉ Owner xóa workspace.
- Admin không được nâng ai thành Owner.
- Không thể hạ cấp hoặc xóa Owner cuối cùng.
- Người ngoài workspace nhận `403` hoặc `404` theo một quy ước nhất quán.

**Tests bắt buộc:** role matrix, duplicate member, invitation hết hạn, last-owner invariant, user ngoài workspace.

**Học từ source:** `ProjectService/Service.cs`, `ProjectRoleExtensions.cs`, `ProjectRoleAuthorization.cs`, `AuthTests.cs`.

**Không làm:** JWT, password, refresh token, document.

### Mini 4 — Markdown Knowledge Publisher

**Sản phẩm:** công cụ lưu bài hướng dẫn có cấu trúc và xuất Markdown chuẩn cho Git repository hoặc static site.

**Tables:**

| Table | Cột chính | Constraint quan trọng |
|---|---|---|
| `articles` | `id`, `article_key`, `title`, `summary`, `notes_md`, timestamps | `article_key` unique. |
| `article_sections` | `id`, `article_id`, `section_type`, `heading`, `content`, `order_index` | Unique theo article/type/order. |
| `article_references` | `id`, `source_article_id`, `target_key`, `label`, `order_index` | Chống self-reference; cho phép dangling reference. |
| `rendered_artifacts` | `id`, `article_id`, `content_hash`, `markdown`, `renderer_version`, `created_at` | Hash + renderer version có index. |

**API tối thiểu:** CRUD article, replace sections, replace references, preview Markdown, tạo artifact, download `.md`.

**Luật nghiệp vụ:**

- Database là source of truth; file `.md` chỉ là output.
- Renderer nhận DTO/snapshot thuần, không query database.
- Cùng input phải cho output giống từng ký tự.
- Hash tính từ nội dung chuẩn hóa, không tính timestamps hoặc renderer output.
- Heading, bullet, numbered list và code fence phải escape đúng.

**Tests bắt buộc:** golden file, serialize/deserialize round-trip, hash ổn định, path chuẩn, dangling reference không tạo link hỏng.

**Học từ source:** toàn bộ `MarkdownService`, `RenderTests.cs`, `SitemapBuilderTests.cs`, các file trong `Golden/`.

**Không làm:** lifecycle, release history, GitHub API. Artifact ở đây chỉ phục vụ học deterministic rendering.

## 6. Cấp 2 — Nghiệp vụ và reliability

### Mini 5 — User Story Planner

**Sản phẩm:** backend cho BA viết User Story có flow và acceptance criteria chuẩn Given/When/Then.

**Tables:**

| Table | Cột chính | Constraint quan trọng |
|---|---|---|
| `stories` | `id`, `story_key`, `title`, `story_statement`, `priority`, `sprint`, `status`, `notes_md` | Key unique; priority/status có CHECK. |
| `story_flows` | `id`, `story_id`, `flow_type`, `code`, `title`, `order_index` | Một Main Flow; branch code unique. |
| `story_flow_steps` | `id`, `flow_id`, `order_index`, `content` | Order unique trong flow. |
| `acceptance_criteria` | `id`, `story_id`, `code`, `given_text`, `when_text`, `then_text`, `order_index` | Code unique trong story. |
| `criterion_ands` | `id`, `criterion_id`, `order_index`, `content` | FK cascade. |

**API tối thiểu:** CRUD story, replace flows, replace acceptance criteria, preview, list/filter theo sprint/priority/status.

**Luật nghiệp vụ:**

- Main Flow không có code; Alternative dùng `ALT-xx`; Exception dùng `EXC-xx`.
- Mỗi flow có ít nhất một step.
- Mỗi story có tối đa một Main Flow.
- Acceptance criterion có code và không trùng.
- Endpoint replace-section xóa phần cũ và ghi phần mới trong cùng transaction.
- Preview được dựng từ aggregate vừa load, không phụ thuộc Mini 4.

**Tests bắt buộc:** branch code, duplicate AC, replace rollback khi input sai, filter/paging, cascade delete, preview ổn định.

**Học từ source:** `DocumentService`, `DocumentValidator.cs`, các entity `DocumentFlow*`, `AcceptanceCriterion*`, `DocumentTests.cs`.

**Không làm:** auth, multi-tenant project, TDD, Business Rule, release.

### Mini 6 — Policy Version Vault

**Sản phẩm:** registry quản lý chính sách nội bộ với draft, release, lịch sử, diff và restore.

**Tables:**

| Table | Cột chính | Constraint quan trọng |
|---|---|---|
| `policies` | `id`, `policy_key`, `title`, `draft_content`, `lifecycle_state`, `current_version`, `draft_hash`, `released_hash`, `row_version` | Key unique; lifecycle/version nhất quán. |
| `policy_versions` | `id`, `policy_id`, `version_number`, `payload_json`, `rendered_text`, `content_hash`, `base_version_id`, `change_summary`, `created_at` | Unique policy/version; version > 0; row bất biến. |
| `policy_activities` | `id`, `policy_id`, `action`, `from_version`, `to_version`, `detail_json`, `created_at` | Append-only. |

**API tối thiểu:** create/update draft, release, list/get version, diff hai version, restore version vào draft, rerender dry-run.

**Luật nghiệp vụ:**

- Release đọc draft, tạo snapshot JSON, render, hash và tăng version trong một transaction.
- Không release nếu hash bằng bản gần nhất.
- Version cũ không đổi khi draft hoặc renderer thay đổi.
- Restore chỉ ghi version cũ về draft; không xóa history và không tự tạo release mới.
- Re-render đọc payload của version, không đọc draft.
- Optimistic concurrency chặn hai người sửa/release đè nhau.

**Tests bắt buộc:** release thiếu dữ liệu, duplicate release, immutable version, diff list có phần tử chèn giữa, restore, concurrent release, dry-run không ghi DB.

**Học từ source:** `ReleaseService`, `PayloadDiff.cs`, `SnapshotApplier.cs`, `DocumentVersionConfiguration.cs`, `ReleaseTests.cs`, `versioning.md`.

**Không làm:** GitHub, Quartz, project membership, semantic search.

### Mini 7 — Reliable GitHub Publisher

**Sản phẩm:** dịch vụ nhận nội dung Markdown và đẩy đáng tin cậy lên một hoặc nhiều GitHub repositories.

**Tables:**

| Table | Cột chính | Constraint quan trọng |
|---|---|---|
| `publish_documents` | `id`, `document_key`, `markdown`, `content_hash`, `updated_at` | Key unique. |
| `repositories` | `id`, `owner`, `name`, `branch`, `base_path`, `encrypted_token`, `key_version`, `is_active` | Owner/name unique; token không lưu plain text. |
| `publish_jobs` | `id`, `document_id`, `repository_id`, `file_path`, `status`, `attempt_count`, `next_attempt_at`, `locked_by`, `locked_until`, `last_error` | Một active job cho document/repository/hash. |
| `publish_attempts` | `id`, `job_id`, `started_at`, `finished_at`, `outcome`, `http_status`, `error` | Append-only để quan sát retry. |

**API tối thiểu:** upsert publish document, CRUD/test repository, enqueue, list status, manual retry, run worker now.

**Worker:** Quartz hoặc `BackgroundService`; dùng `FOR UPDATE SKIP LOCKED`/cơ chế claim nguyên tử để nhiều instance không lấy trùng job.

**Luật nghiệp vụ:**

- Enqueue và lưu nội dung nằm trong một transaction local.
- Nếu remote content giống nhau sau chuẩn hóa newline thì đóng job mà không commit rỗng.
- Lỗi transient dùng exponential backoff; lỗi permanent fail ngay.
- Lock hết hạn thì worker khác được nhận lại.
- Repository gần hết rate limit chỉ defer job của repository đó; worker tiếp tục repository khác.
- Token mã hóa AES-256 với key version để hỗ trợ rotation.

**Tests bắt buộc:** first publish, no-op publish, transient/permanent failure, retry, two workers, expired lock, rate-limit isolation, token encryption round-trip.

**Học từ source:** `PublishService`, `GitHubService`, `PublishJob`, `PipelineConfigurations.cs`, `PublishTests.cs`.

**Không làm:** release/version. `publish_documents` là input đầy đủ của project này.

## 7. Cấp 3 — Data-intensive và integration

### Mini 8 — Searchable Knowledge Base

**Sản phẩm:** knowledge base tiếng Việt có lexical, typo-tolerant và semantic search.

**Tables:**

| Table | Cột chính | Constraint/index quan trọng |
|---|---|---|
| `knowledge_documents` | `id`, `doc_key`, `title`, `body`, `search_text`, `search_vector`, timestamps | GIN `search_vector`, trigram index title, key unique. |
| `knowledge_chunks` | `id`, `document_id`, `section_path`, `chunk_index`, `content`, `content_hash`, `embedding`, `embedding_model`, `embedded_at` | Unique section/chunk; HNSW vector index. |
| `embedding_jobs` | `id`, `chunk_id`, `status`, `attempt_count`, `next_attempt_at`, `last_error` | Một active job cho mỗi chunk hash. |

**API tối thiểu:** upsert document, search với `Lexical/Semantic/Hybrid`, related documents, index status, reindex.

**Ba mốc triển khai trong cùng project:**

1. Full-text bằng `unaccent` + config `simple`.
2. Trigram fallback khi full-text không có kết quả.
3. Chunk + pgvector + Reciprocal Rank Fusion.

**Luật nghiệp vụ:**

- Chunk theo section và chỉ sinh lại embedding khi `content_hash` đổi.
- Section bị xóa phải xóa chunk cũ.
- Embedding provider lỗi thì hybrid tự hạ xuống lexical và báo nhánh đã chạy.
- Vector result phải có distance threshold; “gần nhất” không đồng nghĩa “liên quan”.
- Model name và vector dimension phải khớp migration.

**Tests bắt buộc:** tìm không dấu, typo fallback, chunk unchanged không re-embed, changed section chỉ re-embed phần đó, embedding outage, RRF sort, irrelevant query trả rỗng.

**Học từ source:** `SearchService`, `DocumentChunker.cs`, `ChunkSynchronizer.cs`, `SearchOptions.cs`, `SearchTests.cs`, `SemanticQualityTests.cs`.

**Không làm:** lấy document từ mini khác. API ingest của project này tự sở hữu dữ liệu knowledge.

### Mini 9 — Dependency Impact Analyzer

**Sản phẩm:** quản lý dependency giữa service, module hoặc tài liệu và trả lời “thay đổi node này ảnh hưởng gì?”.

**Tables:**

| Table | Cột chính | Constraint quan trọng |
|---|---|---|
| `artifacts` | `id`, `artifact_key`, `artifact_type`, `title`, `status` | Key unique. |
| `artifact_links` | `id`, `source_id`, `target_id`, `target_key`, `link_type`, `note` | Chống self-link và duplicate edge; target có thể unresolved. |
| `impact_runs` | `id`, `root_artifact_id`, `max_depth`, `result_json`, `created_at` | Lưu report để audit/demo. |

**API tối thiểu:** CRUD artifact, replace links, incoming links, graph, impact report, broken-link report.

**Luật nghiệp vụ:**

- Chỉ `DependsOn`, `Implements`, `GovernedBy`, `Blocks` lan truyền ảnh hưởng.
- `References` và `RelatedTo` chỉ để điều hướng.
- Traversal không treo khi có cycle và tôn trọng `maxDepth`.
- Phân biệt target chưa tồn tại, target archived và link type không hợp lệ.
- Response graph có node degree và cả unresolved edge.

**Tests bắt buộc:** multi-hop, cycle, max depth, non-propagating edge, dangling target, archived target, duplicate/self link.

**Học từ source:** `GraphService`, `DocumentLink`, `DocumentRelationConfigurations.cs`, `GraphTests.cs`.

**Không làm:** document editor, auth, search. `artifacts` là domain độc lập.

### Mini 10 — API Contract Registry

**Sản phẩm:** registry lưu contract API nội bộ, mã lỗi và so sánh với OpenAPI file do team phát triển cung cấp.

**Tables:**

| Table | Cột chính | Constraint quan trọng |
|---|---|---|
| `api_services` | `id`, `service_key`, `name`, `owner_team` | Key unique. |
| `api_endpoints` | `id`, `service_id`, `method`, `path`, `summary`, `deprecated` | Method/path unique trong service. |
| `api_error_codes` | `id`, `service_id`, `code`, `http_status`, `description` | Code unique trong service. |
| `comparison_runs` | `id`, `service_id`, `spec_hash`, `started_at`, `completed_at`, `status` | Hash index để nhận cùng spec. |
| `comparison_items` | `id`, `run_id`, `kind`, `method`, `path`, `detail` | Kind thuộc missing/extra/mismatch. |

**API tối thiểu:** CRUD service/endpoint/error code, upload OpenAPI JSON/YAML, compare, get comparison history, global error-code registry.

**Luật nghiệp vụ:**

- Normalize path và HTTP method trước khi so sánh.
- Chia kết quả thành `documentedOnly`, `specOnly`, `mismatched`.
- OpenAPI sai cú pháp trả `400`, không tạo run hoàn tất giả.
- Error code trùng nội dung có thể hợp lệ; cùng code nhưng status/meaning khác là conflict.
- Lưu hash thay vì lưu lại file giống hệt nhiều lần.

**Tests bắt buộc:** JSON/YAML, malformed spec, three-way comparison, path normalization, error-code conflict, duplicate run.

**Học từ source:** `GraphService.CompareWithOpenApiAsync`, các entity `ApiEndpoint`, `ApiErrorCode`, `GraphTests.cs`.

**Không làm:** TDD document hay project RBAC. Registry tự sở hữu services và contracts.

### Mini 11 — Bulk Import/Export Hub

**Sản phẩm:** nhập/xuất catalog sản phẩm hoặc knowledge records bằng CSV/JSON/ZIP, có dry run và báo lỗi từng dòng.

**Tables:**

| Table | Cột chính | Constraint quan trọng |
|---|---|---|
| `catalog_items` | `id`, `external_key`, `name`, `category`, `content`, `is_locked`, timestamps | External key unique. |
| `import_batches` | `id`, `file_name`, `file_hash`, `mode`, `status`, counters, timestamps | File hash + mode dùng chống chạy trùng. |
| `import_rows` | `id`, `batch_id`, `row_number`, `external_key`, `status`, `error_code`, `error_detail` | Unique batch/row. |
| `export_jobs` | `id`, `format`, `filter_json`, `status`, `file_path`, `expires_at` | File có thời hạn. |

**API tối thiểu:** upload dry-run, execute import, get batch report, retry failed rows, create export, download export.

**Luật nghiệp vụ:**

- Dry run parse và validate nhưng không ghi `catalog_items`.
- Lỗi một dòng không làm mất báo cáo của các dòng còn lại.
- Không overwrite item `is_locked = true`.
- Chống ZIP Slip bằng cách resolve full path và kiểm tra vẫn nằm trong temp root.
- Giới hạn số entry, compressed size, uncompressed size và compression ratio.
- Cùng file hash + mode có idempotency rõ ràng.
- Export tạo manifest chứa schema version và checksum từng file.

**Tests bắt buộc:** dry run, partial failure, duplicate file, locked item, malformed CSV/JSON, ZIP Slip, zip bomb limits, export/import round-trip.

**Học từ source:** `ImportService`, `ExportService`, `MarkdownParser.cs`, `ExportImportTests.cs`, `document_first.Exporter`.

**Không làm:** User Story/TDD/Business Rule. Dùng catalog domain nhỏ để tập trung vào pipeline file.

## 8. Lộ trình đề xuất cho intern

Không cần hoàn thành project trước để project sau chạy. Thứ tự chỉ tối ưu lượng kiến thức phải học cùng lúc.

1. **Mini 1:** học request → validation → EF Core → PostgreSQL → response → test.
2. **Mini 3:** thêm authorization nhưng chưa phải xử lý cryptography/JWT.
3. **Mini 2:** học security sau khi CRUD và transaction đã chắc.
4. **Mini 4:** học pure functions, snapshot DTO và golden tests.
5. **Mini 5:** học aggregate có nhiều bảng con và transaction replace-section.
6. **Mini 6:** học immutable history, hash, diff và state machine.
7. **Mini 7:** học external gateway, background worker và failure recovery.
8. **Mini 9:** học graph traversal trước khi bước vào search phức tạp.
9. **Mini 8:** học PostgreSQL extensions, ranking và quality measurement.
10. **Mini 10:** học parse/normalize/compare contract.
11. **Mini 11:** học file safety, batch processing và idempotency.

Mỗi lần chuyển bài, tạo solution và database mới. Không copy nguyên project trước rồi xóa bớt; cách đó giữ lại dependency ẩn và làm bạn tưởng mình đã hiểu phần hạ tầng.

## 9. Definition of Done cho từng mini-project

Một mini-project chỉ hoàn thành khi đạt đủ:

- `docker compose up -d` dựng được database riêng.
- `dotnet ef database update` chạy trên database rỗng.
- `dotnet test` tự chạy được, không cần project khác đang mở.
- Swagger có một demo flow từ đầu đến cuối.
- Có seed data và lệnh reset local database.
- Mọi table có PK; quan hệ có FK; uniqueness có UNIQUE index.
- Domain invariant quan trọng có cả validation dễ hiểu và database constraint khi phù hợp.
- Error response có `status`, `messageCode`, `detail`, `traceId` nhất quán.
- External dependency có interface và fake cho tests.
- README ghi rõ mục tiêu, schema, cách chạy, demo và những gì cố ý không làm.
- `rg "ProjectReference"` không tìm thấy tham chiếu tới mini-project khác.

## 10. Bản đồ source để tra cứu, không để copy nguyên khối

| Muốn học | Đọc các file |
|---|---|
| API startup và DI | `document_first.API/Program.cs` |
| Error envelope và middleware | `document_first.Service/Models/ApiResponse.cs`, `document_first.API/Middleware/` |
| Auth/session | `document_first.Service/AuthService/`, `JwtService/`, `document_first.Repo/Configurations/AuthConfigurations.cs` |
| RBAC | `document_first.Service/ProjectService/`, `document_first.API/Authorization/` |
| Structured CRUD | `document_first.Service/DocumentService/`, `document_first.Repo/Entity/Document*.cs` |
| Markdown/snapshot | `document_first.Service/MarkdownService/`, `document_first.Tests/Golden/` |
| Release/version | `document_first.Service/ReleaseService/`, `documentation/versioning.md` |
| Outbox/GitHub | `document_first.Service/PublishService/`, `GitHubService/`, `document_first.API/Jobs/PublishWorkerJob.cs` |
| Search | `document_first.Service/SearchService/`, `document_first.Repo/Configurations/PipelineConfigurations.cs` |
| Graph/OpenAPI | `document_first.Service/GraphService/` |
| Import/export | `document_first.Service/ImportService/`, `ExportService/`, `document_first.Exporter/` |
| Database constraints | `document_first.Repo/Configurations/`, `document_first.Tests/SchemaTests.cs` |
| Test infrastructure | `document_first.Tests/Infrastructure/`, `documentation/testing.md` |
| Known trade-offs | `documentation/implementation.md`, `documentation/review.md`, `documentation/enhance.md` |

## 11. Quy tắc học để không biến mini-project thành project lớn lần nữa

- Mỗi bài chỉ chọn **một câu hỏi khó chính**. Ví dụ Mini 7 hỏi “làm sao publish đáng tin cậy?”, không đồng thời học auth và graph.
- Khi thấy cần gọi project khác, tạo model tối giản ngay trong project hiện tại.
- Không tạo abstraction trước khi có ít nhất hai implementation thật.
- Không thêm Redis, Kafka, Elasticsearch hoặc microservice chỉ để “giống production”; PostgreSQL và một process worker đã đủ học pattern cốt lõi.
- Viết integration test cho invariant trước khi thêm endpoint phụ.
- Sau MVP mới làm stretch goal; không trộn stretch goal vào Definition of Done.
- Sau mỗi bài, viết một trang “quyết định và trade-off” bằng lời của bạn. Nếu giải thích được vì sao chọn schema và transaction boundary, bạn đã học được nhiều hơn việc chỉ làm API chạy.

Điểm bắt đầu tốt nhất là **Mini 1 — Helpdesk Ticket API**: nó đủ thực tế để đưa vào portfolio, nhưng không bắt bạn hiểu JWT, pgvector, graph và background worker trong cùng tuần.
