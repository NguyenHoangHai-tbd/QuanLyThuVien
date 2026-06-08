# QLyThuVien - Dự án quản lý thư viện 

## 1. Giới thiệu dự án

`QLyThuVien` là dự án Web API quản lý thư viện được xây dựng bằng .NET, tổ chức theo hướng nhiều tầng gồm `Api`, `Application`, `Domain` và `Infrastructure`. Trong quá trình hoàn thiện, dự án đã được chỉnh sửa lại để gần với cấu trúc dự án thực tế hơn: tách rõ nghiệp vụ theo `Features`, áp dụng CQRS với MediatR, thêm phân quyền JWT, bổ sung migration database và tạo sẵn file Postman để kiểm thử API.

Dự án phục vụ các nghiệp vụ chính của một hệ thống thư viện:

- Đăng nhập, đăng xuất bằng JWT.
- Quản lý tenant/thư viện.
- Quản lý chi nhánh thư viện.
- Quản lý người dùng và phân quyền.
- Quản lý sách.
- Quản lý bản sao sách.
- Quản lý độc giả.
- Quản lý mượn sách, trả sách, gia hạn sách.
- Quản lý đặt giữ sách.
- Xem dashboard thống kê.
- Xem thông báo.
- Xem audit log.
- Kiểm tra kết nối database.

Mục tiêu chính của lần hoàn thiện này là biến project từ dạng code gộp service/DTO/query sang cấu trúc rõ ràng hơn theo CQRS/MediatR, phù hợp để nộp bài hoặc đưa lên GitHub.

## 2. Công nghệ sử dụng

Dự án sử dụng các công nghệ chính:

- .NET 10.
- ASP.NET Core Web API.
- MediatR.
- Entity Framework Core.
- SQL Server LocalDB.
- JWT Bearer Authentication.
- SignalR.
- Postman Collection để test API.

Một số package quan trọng:

- `MediatR`
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Design`
- `Microsoft.Data.SqlClient`

## 3. Cấu trúc solution

Solution chính:

```text
QLyThuVien.slnx
```

Dự án được chia thành 4 project:

```text
QLyThuVien.Api
QLyThuVien.Application
QLyThuVien.Domain
QLyThuVien.Infrastructure
```

Ý nghĩa từng project:

```text
QLyThuVien.Api
```

Là tầng nhận request từ Postman/frontend. Tầng này chứa controller, middleware, cấu hình JWT, SignalR hub và file cấu hình chạy API.

```text
QLyThuVien.Application
```

Là tầng xử lý nghiệp vụ chính. Tầng này chứa CQRS, MediatR command/query/handler, DTO/request/response, interface và các class common dùng chung.

```text
QLyThuVien.Domain
```

Là tầng cốt lõi nghiệp vụ. Tầng này chỉ chứa `Entities` và `Enums`, ví dụ `Book`, `Loan`, `Tenant`, `Branch`, `UserAccount`, `LoanStatus`, `UserRole`.

```text
QLyThuVien.Infrastructure
```

Là tầng hạ tầng. Tầng này chứa database context, migration, repository demo, service tạo token, hash mật khẩu, current user context và kiểm tra database.

## 4. Cấu trúc thư mục hiện tại

Sau khi hoàn thiện, cấu trúc `Application/Features` đã được tách rõ theo từng nghiệp vụ:

```text
QLyThuVien.Application
  Common
  DependencyInjection
  Features
    Ai
    AuditLogs
    Auth
    BookCopies
    Books
    Branches
    Dashboard
    Holds
    Loans
    Members
    Notifications
    Policies
    System
    Tenants
    Users
  Interfaces
```

Mỗi feature được tổ chức theo kiểu:

```text
FeatureName
  Commands
  Common
  Handlers
  Queries
```

Trong đó:

- `Commands`: chứa các thao tác làm thay đổi dữ liệu như create, update, delete, login, logout, return, renew.
- `Queries`: chứa các thao tác chỉ đọc dữ liệu.
- `Handlers`: chứa logic xử lý command/query.
- `Common`: chứa DTO, request, response của feature đó.

Ví dụ feature `Books`:

```text
Books
  Commands
    Create
    Update
    Delete
  Common
    BookDto.cs
    BookListItemDto.cs
    CreateBookRequest.cs
    UpdateBookRequest.cs
  Handlers
    BooksHandler.cs
  Queries
    GetBookQuery.cs
    SearchBooksQuery.cs
```

## 5. Quá trình hoàn thiện dự án

### Bước 1: Rà soát cấu trúc ban đầu

Ban đầu dự án còn nhiều phần code bị gộp chung. Một số DTO được để chung trong một file lớn, các service xử lý nghiệp vụ nằm trực tiếp trong `Application/Services`, queries có lúc được để tách riêng ngoài feature, và một số folder chưa đúng theo yêu cầu cấu trúc CQRS/MediatR.

Sau khi rà soát, hướng chỉnh sửa được chọn là:

- Tách rõ từng tầng `Api`, `Application`, `Domain`, `Infrastructure`.
- Không dùng folder `Abstractions`.
- Chuyển interface sang folder `Interfaces`.
- Tách `Command`, `Query`, `Handler`, `Common` theo từng feature.
- Không gộp nhiều DTO trong một file.
- Không gộp nhiều nghiệp vụ khác nhau trong một feature nếu có thể tách rõ.

### Bước 2: Chuẩn hóa Domain layer

Trong `Domain`, project được chỉnh lại để chỉ còn:

```text
Entities
Enums
```

Các entity chính gồm:

- `Tenant`
- `Branch`
- `LibraryPolicy`
- `UserAccount`
- `Author`
- `Category`
- `Publisher`
- `Book`
- `BookCopy`
- `MemberProfile`
- `Loan`
- `HoldRequest`
- `NotificationMessage`
- `AuditLog`
- `AiUsageLog`

Các enum được tách thành từng file riêng:

- `UserRole.cs`
- `BookCopyStatus.cs`
- `LoanStatus.cs`
- `HoldStatus.cs`
- `MemberStatus.cs`
- `NotificationStatus.cs`

File base entity được đặt ở:

```text
QLyThuVien.Domain/Entities/Entity.cs
```

File này chứa các lớp nền:

- `Entity`
- `AuditableEntity`
- `TenantEntity`
- `BranchEntity`

Mục đích là để các entity khác không phải lặp lại các thuộc tính như `Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, `TenantId`, `BranchId`.

### Bước 3: Chuyển từ service gộp sang CQRS/MediatR

Trước khi chỉnh, nhiều logic nghiệp vụ nằm trong các service như:

- `AuthService`
- `CatalogService`
- `CirculationService`
- `DashboardService`
- `MemberService`
- `TenantService`
- `UserService`
- `AiService`

Sau khi chỉnh, các service nghiệp vụ này được thay bằng:

- `Command`
- `Query`
- `Handler`

Ví dụ login:

```text
Features/Auth
  Commands
    Login
      LoginCommand.cs
    Logout
      LogoutCommand.cs
  Common
    LoginRequest.cs
    LoginResponse.cs
    LogoutResponse.cs
    UserContextDto.cs
  Handlers
    LoginCommandHandler.cs
    LogoutCommandHandler.cs
```

Controller không gọi service trực tiếp nữa. Controller chỉ gửi command/query thông qua MediatR:

```csharp
_sender.Send(new LoginCommand(request), cancellationToken);
```

Cách này giúp controller gọn hơn và logic nghiệp vụ nằm đúng trong handler.

### Bước 4: Tách DTO, request, response vào Common của từng feature

Trước đó DTO bị gộp chung. Sau khi chỉnh, DTO/request/response được tách vào `Common` của từng feature.

Ví dụ:

```text
Features/Auth/Common/LoginRequest.cs
Features/Auth/Common/LoginResponse.cs
Features/Auth/Common/LogoutResponse.cs
```

```text
Features/Books/Common/BookDto.cs
Features/Books/Common/CreateBookRequest.cs
Features/Books/Common/UpdateBookRequest.cs
```

```text
Features/Loans/Common/LoanDto.cs
Features/Loans/Common/LoanRequest.cs
Features/Loans/Common/ReturnRequest.cs
Features/Loans/Common/RenewRequest.cs
```

Mục đích là khi nhìn vào một feature, có thể biết ngay feature đó cần request/response nào.

### Bước 5: Tách Commands theo từng hành động cụ thể

Các command không còn để chung một thư mục lớn. Mỗi thao tác được đặt trong folder riêng:

```text
Commands
  Create
  Update
  Delete
```

Ví dụ với sách:

```text
Features/Books/Commands/Create/CreateBookCommand.cs
Features/Books/Commands/Update/UpdateBookCommand.cs
Features/Books/Commands/Delete/DeleteBookCommand.cs
```

Ví dụ với lượt mượn:

```text
Features/Loans/Commands/Create/CreateLoanCommand.cs
Features/Loans/Commands/Return/ReturnLoanCommand.cs
Features/Loans/Commands/Renew/RenewLoanCommand.cs
```

Cách này giúp cấu trúc rõ hơn khi số lượng command tăng lên.

### Bước 6: Đưa Queries vào lại feature tương ứng

Queries không để chung bên ngoài nữa. Mỗi query nằm trong feature của chính nó.

Ví dụ:

```text
Features/Books/Queries/SearchBooksQuery.cs
Features/Books/Queries/GetBookQuery.cs
```

```text
Features/Loans/Queries/GetLoansQuery.cs
```

```text
Features/Dashboard/Queries/GetDashboardSummaryQuery.cs
```

Điều này giúp query không bị tách rời khỏi nghiệp vụ mà nó phục vụ.

### Bước 7: Tách các feature đang bị gộp

Một phần quan trọng trong quá trình hoàn thiện là tách các feature đang bị gộp chung.

Trước đó:

```text
Catalog
  Books
  Copies
```

Đã tách thành:

```text
Books
BookCopies
```

Trước đó:

```text
Circulation
  Loans
  Holds
```

Đã tách thành:

```text
Loans
Holds
```

Trước đó:

```text
Operations
  Dashboard
  Notifications
  AuditLogs
```

Đã tách thành:

```text
Dashboard
Notifications
AuditLogs
```

Trước đó:

```text
Tenants
  Tenants
  Branches
  Policies
```

Đã tách thành:

```text
Tenants
Branches
Policies
```

Sau khi tách, danh sách feature hiện tại là:

```text
Ai
AuditLogs
Auth
BookCopies
Books
Branches
Dashboard
Holds
Loans
Members
Notifications
Policies
System
Tenants
Users
```

Các route API vẫn được giữ nguyên để không làm hỏng cách test trên Postman.

### Bước 8: Thêm JWT Authentication và phân quyền

Ban đầu project dùng token tự viết bằng Base64 và HMAC. Sau đó đã chuyển sang JWT chuẩn.

Các thay đổi chính:

- Thêm package `Microsoft.AspNetCore.Authentication.JwtBearer`.
- Thêm cấu hình `Jwt` trong `appsettings.json`.
- Cập nhật `AccessTokenService` để tạo JWT thật.
- Cập nhật `Program.cs` để dùng `AddAuthentication`, `AddJwtBearer`, `UseAuthentication`, `UseAuthorization`.
- Cập nhật `TenantContextMiddleware` để đọc claims từ JWT đã được ASP.NET Core xác thực.
- Gắn `[Authorize]` và `[Authorize(Roles = "...")]` cho controller.

Token sau khi login có dạng JWT chuẩn:

```text
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

Một số phân quyền hiện tại:

- `UsersController`: `SuperAdmin`, `TenantAdmin`.
- `SystemController`: `SuperAdmin`, `TenantAdmin`.
- `MembersController`: `SuperAdmin`, `TenantAdmin`, `Librarian`.
- `CirculationController`: `SuperAdmin`, `TenantAdmin`, `Librarian`.
- `CatalogController`: đăng nhập là xem được, thêm/sửa/xóa cần quyền quản lý.
- `AiController`: chỉ cần đăng nhập.
- `AuthController`: login không cần token, logout cần token.

### Bước 9: Bổ sung middleware

Project hiện có 2 middleware tự viết:

```text
QLyThuVien.Api/Middleware/ExceptionHandlingMiddleware.cs
QLyThuVien.Api/Middleware/TenantContextMiddleware.cs
```

`ExceptionHandlingMiddleware` có nhiệm vụ bắt lỗi toàn API và trả lỗi dạng JSON.

`TenantContextMiddleware` có nhiệm vụ đọc thông tin user/tenant từ JWT claims, kiểm tra tenant và user còn active hay không, sau đó set `CurrentUserContext` để handler sử dụng.

Thứ tự middleware trong `Program.cs`:

```csharp
app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseMiddleware<TenantContextMiddleware>();
app.UseAuthorization();
```

### Bước 10: Bổ sung Infrastructure và migration

Trong `Infrastructure`, các phần chính gồm:

```text
DependencyInjection
Migrations
Persistence
Services
```

`Services` chứa:

- `AccessTokenService.cs`
- `CurrentUserContext.cs`
- `Sha256PasswordHasher.cs`
- `SystemClock.cs`

`Persistence` chứa:

- `InMemoryLibraryRepository.cs`
- `LibraryDbContext.cs`
- `LibraryDbContextFactory.cs`
- `SqlServerDatabaseConnectionChecker.cs`

`Migrations` chứa migration EF Core:

```text
20260518001537_InitialCreate.cs
20260518001537_InitialCreate.Designer.cs
LibraryDbContextModelSnapshot.cs
```

Migration được đặt ngoài `Persistence` theo yêu cầu cấu trúc thư mục.

## 6. Cách chạy dự án

### Bước 1: Mở terminal tại thư mục project

```powershell
cd C:\Baitap\QLyThuVien
```

### Bước 2: Restore package

```powershell
dotnet restore .\QLyThuVien.slnx
```

### Bước 3: Build toàn bộ solution

```powershell
dotnet build .\QLyThuVien.slnx
```

Kết quả mong muốn:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

### Bước 4: Chạy API

```powershell
dotnet run --project .\QLyThuVien.Api\QLyThuVien.Api.csproj --launch-profile http
```

API chạy tại:

```text
http://localhost:5084
```

Nếu chạy profile https thì có thể dùng:

```text
https://localhost:7241
```

## 7. Cấu hình database

Connection string hiện nằm trong:

```text
QLyThuVien.Api/appsettings.Development.json
```

Nội dung chính:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=QLyThuVienDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

Database dùng:

```text
QLyThuVienDb
```

Server dùng:

```text
(localdb)\MSSQLLocalDB
```

## 8. Lệnh migration và update database

Nếu cần tạo migration mới:

```powershell
dotnet ef migrations add InitialCreate --project .\QLyThuVien.Infrastructure\QLyThuVien.Infrastructure.csproj --startup-project .\QLyThuVien.Api\QLyThuVien.Api.csproj --output-dir Migrations
```

Nếu cần update database:

```powershell
dotnet ef database update --project .\QLyThuVien.Infrastructure\QLyThuVien.Infrastructure.csproj --startup-project .\QLyThuVien.Api\QLyThuVien.Api.csproj
```

Kết quả đã kiểm tra:

```text
Build succeeded.
No migrations were applied. The database is already up to date.
Done.
```

Nếu gặp cảnh báo EF tools khác version runtime, ví dụ:

```text
The Entity Framework tools version '10.0.7' is older than that of the runtime '10.0.8'
```

Đây là cảnh báo, không phải lỗi update database. Có thể cập nhật `dotnet-ef` sau nếu cần.

## 9. Dữ liệu demo

Dữ liệu demo hiện được seed trong:

```text
QLyThuVien.Infrastructure/Persistence/InMemoryLibraryRepository.cs
```

Tài khoản demo:

```text
Tenant key: pacific
Email: admin@pacific.edu.vn
Password: Admin@123
Role: TenantAdmin
```

```text
Tenant key: pacific
Email: librarian@pacific.edu.vn
Password: Library@123
Role: Librarian
```

Một số ID demo:

```text
tenantId = aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa
branchId = bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1
memberId = dddddddd-dddd-dddd-dddd-ddddddddddd1
```

Lưu ý: nghiệp vụ hiện đang chạy bằng `InMemoryLibraryRepository`, nên nếu tắt API rồi chạy lại thì dữ liệu thêm bằng Postman sẽ quay về dữ liệu seed ban đầu. Phần EF Core migration và database đã được bổ sung để thể hiện cấu trúc database, nhưng repository nghiệp vụ hiện tại vẫn là bản demo in-memory.

## 10. Test bằng Postman

Đã tạo sẵn 2 file JSON để import vào Postman:

```text
postman/QLyThuVien.postman_collection.json
postman/QLyThuVien.postman_environment.json
```

Cách import:

1. Mở Postman.
2. Chọn `Import`.
3. Import 2 file JSON trong thư mục `postman`.
4. Chọn environment `QLyThuVien Local`.
5. Chạy API bằng lệnh:

```powershell
dotnet run --project C:\Baitap\QLyThuVien\QLyThuVien.Api\QLyThuVien.Api.csproj --launch-profile http
```

6. Chạy request đầu tiên:

```text
01 Auth -> Login Admin - Save JWT Token
```

Request này sẽ tự lưu JWT vào biến:

```text
token
```

Sau đó các request còn lại sẽ tự dùng:

```text
Authorization: Bearer {{token}}
```

## 11. Các API chính để test

### Auth

Login:

```http
POST /api/auth/login
```

Body:

```json
{
  "tenantKey": "pacific",
  "email": "admin@pacific.edu.vn",
  "password": "Admin@123"
}
```

Logout:

```http
POST /api/auth/logout
```

### Tenants

```http
GET /api/tenants/current
GET /api/tenants
POST /api/tenants
PUT /api/tenants/{id}
DELETE /api/tenants/{id}
```

### Branches

```http
GET /api/branches
POST /api/branches
PUT /api/branches/{id}
DELETE /api/branches/{id}
```

### Policies

```http
GET /api/policies/current
```

### Users

```http
GET /api/users
GET /api/users/{id}
POST /api/users
PUT /api/users/{id}
DELETE /api/users/{id}
```

### Books

```http
GET /api/catalog/books
GET /api/catalog/books?search=clean
GET /api/catalog/books/{id}
POST /api/catalog/books
PUT /api/catalog/books/{id}
DELETE /api/catalog/books/{id}
```

### BookCopies

```http
GET /api/catalog/copies
POST /api/catalog/copies
PUT /api/catalog/copies/{id}
DELETE /api/catalog/copies/{id}
```

### Members

```http
GET /api/members
GET /api/members/{id}
POST /api/members
PUT /api/members/{id}
DELETE /api/members/{id}
```

### Loans

```http
GET /api/circulation/loans
GET /api/circulation/loans?activeOnly=true
POST /api/circulation/loans
POST /api/circulation/returns
POST /api/circulation/renewals
```

### Holds

```http
GET /api/circulation/holds
POST /api/circulation/holds
POST /api/circulation/holds/{id}/cancel
```

### Dashboard

```http
GET /api/dashboard/summary
```

### Notifications

```http
GET /api/notifications
GET /api/notifications?branchId={branchId}
```

### AuditLogs

```http
GET /api/audit-logs
GET /api/audit-logs?branchId={branchId}
```

### AI

```http
POST /api/ai/search
POST /api/ai/chat
```

AI search body:

```json
{
  "query": "clean code"
}
```

AI chat body:

```json
{
  "message": "He thong hien co bao nhieu sach va luot muon?"
}
```

### System

```http
GET /api/system/database
```

## 12. Test phân quyền JWT

### Test 401 Unauthorized

Gọi API không gửi token:

```http
GET /api/users
```

Kết quả mong muốn:

```text
401 Unauthorized
```

### Test 403 Forbidden

Login bằng tài khoản librarian:

```json
{
  "tenantKey": "pacific",
  "email": "librarian@pacific.edu.vn",
  "password": "Library@123"
}
```

Sau đó gọi:

```http
GET /api/users
```

Kết quả mong muốn:

```text
403 Forbidden
```

Lý do: `Librarian` không có quyền quản lý user. API `/api/users` chỉ cho `SuperAdmin` hoặc `TenantAdmin`.

Login lại bằng admin:

```json
{
  "tenantKey": "pacific",
  "email": "admin@pacific.edu.vn",
  "password": "Admin@123"
}
```

Gọi lại:

```http
GET /api/users
```

Kết quả mong muốn:

```text
200 OK
```

## 13. Các lệnh đã dùng để kiểm tra

Build solution:

```powershell
dotnet build .\QLyThuVien.slnx
```

Update database:

```powershell
dotnet ef database update --project .\QLyThuVien.Infrastructure\QLyThuVien.Infrastructure.csproj --startup-project .\QLyThuVien.Api\QLyThuVien.Api.csproj
```

Chạy API:

```powershell
dotnet run --project .\QLyThuVien.Api\QLyThuVien.Api.csproj --launch-profile http
```

Kiểm tra nhanh bằng Postman:

```text
01 Auth -> Login Admin - Save JWT Token
07 Books -> Search Books - Save bookId
08 BookCopies -> Get Copies - Save copyId and barcode
10 Loans -> Get Loans - Save loanId
11 Holds -> Get Holds - Save holdId
12 Dashboard -> Dashboard Summary
15 AI -> AI Search
16 JWT Authorization Tests -> Without Token - Should Return 401
16 JWT Authorization Tests -> Librarian Get Users - Should Return 403
```

## 14. Kết quả kiểm thử

Các kiểm thử đã thực hiện:

- Build solution thành công.
- Update database thành công.
- Login trả JWT đúng chuẩn.
- API dùng Bearer token gọi được.
- AI search chạy được.
- Dashboard trả dữ liệu thống kê.
- Books, BookCopies, Loans, Holds, Branches, Policies, Notifications, AuditLogs gọi được sau khi tách feature.
- Tài khoản `Librarian` gọi `/api/users` bị chặn `403`, đúng phân quyền.
- Không gửi token thì API trả `401`.

## 15. Ghi chú khi đưa lên GitHub

Trước khi push lên GitHub, nên kiểm tra lại:

```powershell
dotnet build .\QLyThuVien.slnx
```

Nếu API đang chạy và build bị lỗi do file DLL đang bị khóa, hãy tắt API hoặc dừng process đang chạy rồi build lại.

Các thư mục không nên đưa lên GitHub đã được khai báo trong `.gitignore`, ví dụ:

```text
bin/
obj/
.vs/
*.user
*.log
```

Thư mục nên đưa lên GitHub:

```text
QLyThuVien.Api
QLyThuVien.Application
QLyThuVien.Domain
QLyThuVien.Infrastructure
postman
README.md
QLyThuVien.slnx
```

## 16. Tổng kết

Dự án đã được hoàn thiện theo hướng rõ ràng hơn:

- Có kiến trúc 4 tầng.
- Có CQRS/MediatR.
- Có command/query/handler theo từng feature.
- Có DTO/request/response tách riêng trong `Common`.
- Có domain chỉ gồm `Entities` và `Enums`.
- Có interface tách riêng trong `Application/Interfaces`.
- Có JWT Authentication và phân quyền theo role.
- Có migration và DbContext.
- Có Postman collection/environment để test.
- Có README mô tả quá trình hoàn thiện, cách chạy và cách kiểm thử.

Với cấu trúc hiện tại, project dễ đọc hơn, dễ test hơn và thể hiện được quá trình refactor từ code gộp sang cấu trúc CQRS/MediatR theo từng feature.
