# Bản quyền & Nguồn gốc

Repo này KHÔNG chứa bất kỳ tài sản bản quyền nào của Webzen (binary, model 3D, texture, âm thanh, map data). Các file này đã bị loại trừ qua `.gitignore`.

## Nguồn gốc từng phần
- `src/server-s6` (OpenMU): open-source [MUnique OpenMU](https://github.com/MUnique/OpenMU), license MIT.
- `src/client-s6` (MuMain): C++ hook client Season 6 do cộng đồng private-server phát triển.
- `src/server-s16` / `src/client-s16` (LgdMu): source C++ private-server do bên thứ ba (LgdMu) phát triển, không phải file Webzen.
- `reference/s16-data` (ZhyperMU S16 Full, MuOnline_S16_Lgd-main): data + binary client — chứa asset bản quyền Webzen, KHÔNG push.
- `reference/s16-tools` (MuOnline-WorldEditor, MuClientTools16): tool bên thứ ba, KHÔNG push.
- `reference/archives`: 6 file `.zip` gốc, KHÔNG push.
- `src/web-portal`, `src/launcher`, `src/simulation`, `src/database`: tự phát triển trong dự án.

## Cảnh báo
MU Online là tài sản của Webzen. Repo này chỉ phục vụ mục đích học tập/nghiên cứu kỹ thuật.
