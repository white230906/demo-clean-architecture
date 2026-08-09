# Mini Helpdesk Ticket API

REST API quản lý ticket hỗ trợ, label và comment, được xây dựng bằng ASP.NET Core 8 và PostgreSQL.

## Chức năng chính

- Tạo, xem, lọc, phân trang, cập nhật và xóa ticket.
- Gắn lại toàn bộ label cho ticket.
- Thêm comment vào ticket đang mở.
- Quản lý label và tự động tạo slug không trùng.
- Kiểm soát cập nhật đồng thời bằng PostgreSQL `xmin`.
- Chuẩn hóa lỗi API qua middleware và giới hạn tần suất request.
- Swagger UI trong môi trường Development.

## Công nghệ

- .NET 8 / ASP.NET Core Web API
- Entity Framework Core 8
- PostgreSQL / Npgsql
- xUnit
- Swagger / OpenAPI

## Cấu trúc dự án

```text
mini_1_helpdesk_ticket.API/       HTTP API, controller, middleware và cấu hình
mini_1_helpdesk_ticket.Service/   Nghiệp vụ ticket, label và các service hỗ trợ
mini_1_helpdesk_ticket.Repo/      DbContext, entity, migration và truy cập dữ liệu
mini-1-helpdesk-ticket.Test/      Unit test cho service
```

## Chạy dự án

Yêu cầu: .NET SDK 8 và PostgreSQL.

1. Cấu hình connection string bằng biến môi trường PowerShell:

   ```powershell
   $env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=helpdesk;Username=postgres;Password=your_password"
   ```

2. Khôi phục package và cập nhật database:

   ```powershell
   dotnet restore
   dotnet ef database update --project mini_1_helpdesk_ticket.Repo --startup-project mini_1_helpdesk_ticket.API
   ```

3. Chạy API:

   ```powershell
   dotnet run --project mini_1_helpdesk_ticket.API
   ```

Swagger UI mặc định ở `http://localhost:5095/swagger` khi chạy profile HTTP trong môi trường Development.

## Chạy test

```powershell
dotnet test
```

## API chính

| Method | Endpoint | Mô tả |
| --- | --- | --- |
| `GET` | `/api/labels` | Lấy danh sách label |
| `POST` | `/api/labels` | Tạo label |
| `DELETE` | `/api/labels` | Xóa nhiều label |
| `GET` | `/api/tickets` | Lọc và phân trang ticket |
| `GET` | `/api/tickets/{id}` | Lấy chi tiết ticket |
| `POST` | `/api/tickets` | Tạo ticket |
| `PUT` | `/api/tickets/{id}` | Cập nhật ticket |
| `DELETE` | `/api/tickets/{id}` | Xóa ticket |
| `POST` | `/api/tickets/{id}/comments` | Thêm comment |
| `PUT` | `/api/tickets/{id}/labels` | Thay thế danh sách label |

## Sequence diagram

Xem [tài liệu Mermaid của Labels.Service và Tickets.Service](mini_1_helpdesk_ticket.API/Document/service-sequence-diagrams.md), gồm ER diagram database và đầy đủ 10 sequence diagram cho các hàm nghiệp vụ.

## Lưu ý bảo mật tài liệu

Ngoại trừ file sequence diagram được công khai ở trên, các file Markdown khác bên trong thư mục `Document` là tài liệu nội bộ và được loại khỏi Git bằng `.gitignore`.
