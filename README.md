# Video Timeline Marker

He thong Web Application ho tro danh dau, phan tich va quan ly cac moc thoi gian trong video dua tren nen tang ASP.NET MVC 5, Entity Framework 6 va SQL Server LocalDB.

## 1. Gioi thieu tong quan

Ung dung cho phep nguoi dung tai len cac video, tu dong nhan dang thoi luong va tao truc thoi gian tuong ung. Nguoi dung co the danh dau cac khoang thoi gian (segment), phan loai giua cac thao tac binh thuong va cac su co / thao tac thua, dat nhan chu thich va xuat bao cao du lieu duoi dinh dang JSON.

Tat ca thong tin video va cac doan danh dau duoc luu tru lau dai trong co so du lieu SQL Server LocalDB.

## 2. Yeu cau he thong

- He dieu hanh: Windows 10 hoac Windows 11.
- Moi truong phat trien: Visual Studio 2019, 2022 hoac phien ban moi hon (da cai dat workload ASP.NET and web development).
- .NET Framework: 4.7.2 tro len (tuong thich san tren Windows).
- Co so du lieu: SQL Server LocalDB (mac dinh di kem Visual Studio).

## 3. Huong dan cai dat va chay ung dung

### Cach 1: Chay truc tiep bang Visual Studio

1. Mo tap tin VideoTimelineApp.sln bang Visual Studio.
2. Nhan chuot phai vao Solution trong cua so Solution Explorer, chon "Restore NuGet Packages" (hoac Visual Studio se tu dong khoi phuc cac thu vien can thiet khi build).
3. Nhan phim F5 (hoac Ctrl + F5 de chay khong debug).
4. He thong se tu dong khoi tao co so du lieu VideoTimelineDb trong SQL Server LocalDB o lan khoi dong dau tien.

### Cach 2: Khoi phuc goi thu vien bang script PowerShell

1. Mo PowerShell tai thu muc goc cua du an.
2. Chay lenh:
   powershell -ExecutionPolicy Bypass -File .\setup.ps1
3. Mo VideoTimelineApp.sln va nhan F5 de bat dau su dung.

## 4. Huong dan su dung chi tiet

### 4.1. Quan ly thu vien video (Trang chu)

- Tai len video moi:
  - Nhap tieu de video vao o "Ten video" (neu de trong, he thong se tu dong lay ten file goc).
  - Keo tha tap tin video vao khung nhan file hoac bam vao "bam chon file".
  - Cac dinh dang duoc ho tro: MP4, MOV, MKV, AVI, WEBM (dung luong toi da 500 MB).
  - Nhan nut "Upload & Mo Timeline" de tien hanh tai len.
- Xem danh sach video:
  - Cac video da tai len se xuat hien duoi dang the (card) trong muc "Thu vien video".
  - Thong tin hien thi gom: thoi luong, dung luong, so doan da danh dau, thoi diem upload va ban xem truoc cac moc thoi gian.
- Mo giao dien danh dau: Nhan nut "Mo Timeline" tai the video tuong ung.
- Xoa video: Nhan nut thung rac de xoa ca file video vat ly tren may chu lan toan bo du lieu moc thoi gian lien quan trong co so du lieu.

### 4.2. Giao dien danh dau Timeline (Trang Player)

1. Phat video va dieu khien:
   - Su dung thanh dieu khien video mac dinh hoac bam nut "Play/Pause" tren thanh cong cu.
   - Click truc tiep vao bat ky vi tri nao tren thanh thoi gian (Timeline) de tua den giay do.
   - Khi re chuot tren thanh Timeline, tooltip se hien thi thoi gian tuong ung tai con tro.

2. Danh dau mot giai doan:
   - Buoc 1: Chon loai doan:
     - "Binh thuong": Danh dau cac thao tac dung, quy trinh chuan (mau sac tu dong phan bo tren thanh thoi gian).
     - "Su co": Danh dau cac loi, su co hoac thao tac thua (hien thi vien do va hoa tiet soc cheo de de dang nhan dien).
   - Buoc 2: Nhap chu thich vao o "Nhan doan" (khong bat buoc).
   - Buoc 3: Phat video den thoi diem bat dau can ghi nhan, nhan nut "Dat diem dau" (hoac nhan phim S tren ban phim).
   - Buoc 4: Cho video chay (hoac tua) den thoi diem ket thuc, nhan nut "Dat diem cuoi" (hoac nhan phim E tren ban phim).
   - He thong se tu dong kiem tra va luu doan vao co so du lieu thong qua REST API.

3. Co che chong chong cheo (Overlap Prevention):
   - He thong khong cho phep tao cac doan de len nhau.
   - Neu diem dau va diem cuoi du dinh chon nam trong pham vi cua mot doan da co san, he thong se canh bao mau do va chan luu de dam bao tinh chinh xac cua du lieu.

4. Chinh sua va quan ly doan:
   - Chinh sua nhan: Nhan vao bieu tuong cay but tren the doan danh dau, nhap nhan moi trong hop thoai va bam Luu.
   - Xoa tung doan: Nhan vao bieu tuong thung rac tren the doan can xoa.
   - Hoan tac doan cuoi: Nhan nut "Undo" tren thanh cong cu.
   - Xoa tat ca: Nhan nut "Xoa tat ca" tren thanh cong cu de lam moi lai toan bo Timeline cua video do.

5. Xuat du lieu (Export JSON):
   - Nhan nut "Xuat JSON" tren thanh cong cu.
   - He thong se tao mot file .json chua day du so lieu thong ke tong the (tong so doan, so su co, tong thoi gian ghi nhan, ty le bao phu tren tong do dai video) cung danh sach chi tiet tung doan theo tung moc giay cu the.

### 4.3. Cac phim tat ho tro

- Phim Space: Phat hoac tam dung video (Play / Pause).
- Phim S: Dat moc thoi gian bat dau tai vi tri hien tai cua video (Start Point).
- Phim E: Dat moc thoi gian ket thuc va luu doan (End Point).
- Phim Mui ten trai: Tua lui 5 giay.
- Phim Mui ten phai: Tua tien 5 giay.

## 5. Cau truc ma nguon du an

- App_Start:
  - RouteConfig.cs: Cau hinh duong dan MVC.
  - WebApiConfig.cs: Cau hinh REST API va dinh dang JSON (camelCase).
  - FilterConfig.cs: Cau hinh bo loc loi toan cuc.
- Controllers:
  - HomeController.cs: Xu ly hien thi danh sach video, tai len va xoa video.
  - VideoController.cs: Xu ly hien thi trang Player va du lieu khoi tao.
  - SegmentsApiController.cs: Xu ly cac thao tac RESTful CRUD cho segments (GET, POST, PUT, DELETE, PATCH).
- Models:
  - VideoSession.cs: Thuc the luu thong tin video tai len.
  - Segment.cs: Thuc the luu thong tin cac doan moc thoi gian.
- Data:
  - AppDbContext.cs: DbContext cua Entity Framework 6, thiet lap quan he va xoa theo tang (Cascade Delete).
- Views:
  - Home/Index.cshtml: Giao dien quan ly thu vien video.
  - Video/Player.cshtml: Giao dien phat video va danh dau truc thoi gian.
  - Shared/_Layout.cshtml: Giao dien khung giao dien toi (Dark Theme).
- Content/Site.css: Toan bo dinh dang giao dien, thiet ke dap ung (Responsive) cho ca may tinh va thiet bi di dong.
- Scripts/timeline.js: Toan bo logic xu ly phia client (dieu khien truc thoi gian, giao tiep voi API, xuat file).
- uploads/videos: Thu muc luu tru file video vat ly cua nguoi dung.

## 6. Danh sach REST API

- GET /api/segments/{videoId}: Lay danh sach toan bo cac doan danh dau cua video.
- POST /api/segments: Tao mot doan danh dau moi (co xac thuc chong de moc thoi gian o phia may chu).
- PUT /api/segments/{id}: Cap nhat nhan chu thich cua doan theo ID.
- DELETE /api/segments/{id}: Xoa doan khoi co so du lieu theo ID.
- PATCH /api/segments/video/{videoId}/duration: Cap nhat thoi luong chuan xac cua video sau khi trinh duyet doc duoc thong tin metadata.
