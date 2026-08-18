# Hệ Thống Sự Kiện Đại Chiến Phòng Thủ Quái Vật (Wave Defense / Horde Survival Mode)

Tài liệu hướng dẫn sử dụng, tùy chỉnh và nâng cấp lệnh sự kiện `/battle` — Chế độ Liên Minh Các Party 5 Người Bảo Vệ GM (Cọc Tiêu) Khỏi 5 Làn Sóng Quái Vật & Trùm Ma Quỷ, phục vụ quay phim, livestream và tạo nội dung truyền thông cho OpenMU.

---

## 1. Cú Pháp & Lệnh Điều Khiển

| Lệnh | Mô tả chi tiết |
| :--- | :--- |
| `/battle [số_party] [thời_gian_đếm_ngược]` | Bắt đầu sự kiện phòng thủ quái vật.<br>• **Mặc định**: `/battle 4 20` (4 Party = 20 Bot, đếm ngược 20s chuẩn bị góc máy).<br>• **Tùy biến**: `/battle 6 30` (6 Party = 30 Bot, đếm ngược 30s). |
| `/battle stop` hoặc `/battle clear` | **Dọn dẹp chiến trường ngay lập tức**: Xóa sạch toàn bộ quái vật sự kiện trên map, giải tán toàn bộ Party, xóa Bot chiến trường và khôi phục hoạt động cày cuốc bình thường. |

---

## 2. Các Điểm Cải Tiến Nổi Bật

### 🛡️ 1. Tổ Chức Party 5 Người Chuẩn MU
Các Bot tự động tạo nhóm Party 5 người với đầy đủ Party Buff, Party Aura và phối hợp chiêu thức theo 4 công thức tối ưu:
- **Công thức 1 (Balanced Rainbow)**: `Blade Master (DK Combo) + Grand Master (DW Phép) + High Elf (Buff Ene) + Lord Emperor (DL Chiến) + Duel Master (MG Phép)`.
- **Công thức 2 (Striker Burst)**: `Blade Master (DK Combo) + Fist Master (RF Đấm xuyên giáp) + High Elf (Buff Ene) + Dimension Master (SUM Hút máu) + Lord Emperor (DL)`.
- **Công thức 3 (Magic & Ranged)**: `Grand Master (DW AoE Mưa Băng Tuyết) + High Elf (Elf Chiến Bắn Cung Agi) + High Elf (Buff Ene) + Duel Master (MG Phép) + Dimension Master (SUM)`.
- **Công thức 4 (Titan Vanguard)**: `Blade Master (DK Máu Tanker) + Blade Master (DK Combo) + High Elf (Buff Ene) + Fist Master (RF Bão vũ) + Lord Emperor (DL)`.

### ⚡ 2. Điểm Tiềm Năng 40.000 Stats Tối Ưu Từng Nhánh
- **High Elf (Elf Buff)**: `Str 2,000 | Agi 8,000 | Vit 5,000 | Ene 25,000` (Tối đa hóa buff Công/Thủ/Hồi máu).
- **High Elf (Elf Chiến)**: `Str 3,000 | Agi 25,000 | Vit 5,000 | Ene 7,000` (Max tốc độ bắn, né tránh, DPS vật lý).
- **Blade Master (DK Combo)**: `Str 20,000 | Agi 12,000 | Vit 6,000 | Ene 2,000` (Sát thương combo vũ bão).
- **Blade Master (DK Máu Tanker)**: `Str 10,000 | Agi 8,000 | Vit 15,000 | Ene 7,000` (Gồng HP khủng bảo vệ đồng đội).
- **Grand Master (DW Phép)**: `Str 2,000 | Agi 12,000 | Vit 6,000 | Ene 20,000` (Mưa Băng Tuyết & Sấm Sét AoE).
- **Lord Emperor (DL Chiến & Ngựa)**: `Str 15,000 | Agi 10,000 | Vit 5,000 | Ene 6,000 | Cmd 4,000`.
- **Fist Master (RF Đấm Bỏ Qua Giáp)**: `Str 18,000 | Agi 10,000 | Vit 8,000 | Ene 4,000`.
- **Dimension Master (SUM Hút Máu & Giảm Thủ)**: `Str 2,000 | Agi 12,000 | Vit 6,000 | Ene 20,000`.
- **Duel Master (MG Phép Bão Điện)**: `Str 3,000 | Agi 12,000 | Vit 4,000 | Ene 21,000`.

### 💎 3. Trang Bị Max Excellent +15 Đa Dạng
Mỗi class sở hữu pool ngẫu nhiên gồm nhiều bộ Set giáp +15 và vũ khí + cánh cấp 2/3 khác nhau:
- **DK**: Set Dragon Knight, Great Dragon, Brave; Cặp Bone Blade, Great Dragon Sword, Knight Blade + Khiên Rồng; Cánh Wings of Storm.
- **DW**: Set Venom Mist, Grand Soul; Kundun Staff, Destruction Staff, Grand Soul Shield; Cánh Wings of Eternal.
- **Elf**: Set Sylpid Ray, Divine; Sylph Wind Bow, Albatross Bow; Cánh Wings of Illusion.
- **DL**: Set Soleil, Dark Steel; Shining Scepter, Great Lord Scepter + Chiến Mã Dark Horse; Cánh Mantle of Monarch.
- **MG**: Set Volcano, Hurricane; Cặp Explosion Blade, Rune Bastard Sword; Cánh Wings of Ruin.
- **SUM**: Set Storm Jahad, Demonic; Red Wing Stick + Book of Neil; Cánh Wings of Dimension.
- **RF**: Set Phoenix Soul; Cặp Phoenix Soul Star; Cánh Cape of Overrule.

### 🎥 4. GM Làm Cọc Tiêu Trung Tâm (Beacon)
- Các Party bot được xếp thành vành đai phòng thủ bao quanh GM (bán kính 3-5 ô).
- GM là cọc tiêu bất tử: Quái vật và Bot không thể tấn công GM, cho phép GM tự do xoay camera 3D 360 độ (F9) và zoom để ghi lại góc máy hoành tráng.

### 🐲 5. Chuỗi 5 Làn Sóng Quái Vật (5 Escalating Waves)
1. **Đợt 1 (Quỷ Dữ Trinh Sát)**: Bull Fighter, Hound, Skeleton Warrior, Lich, Giant (20 con).
2. **Đợt 2 (Quân Tiên Phong Biển Sâu & Tháp Lạc Lối)**: Gorgon, Silver Valkyrie, Lizard King, Ice Queen, Bahamut (25 con).
3. **Đợt 3 (Quân Đoàn Sa Mạc Tarkan & Rừng Aida)**: Mutant, Iron Wheel, Death Beam Knight, Death Cow, Bloody Wolf (30 con).
4. **Đợt 4 (Binh Đoàn Rồng Đỏ & Quái Vật Hoàng Kim)**: Red Dragon, Golden Dragon, Golden Dark Knight, Golden Devil, Golden Lizard King (25 con).
5. **Đợt 5 (ĐẠI CHIẾN BOSS CUỐI)**: Kundun, Medusa, Zaikan, Balrog, Erohim, Nightmare + Hộ vệ tinh nhuệ.
- Quái vật sử dụng `HordeMonsterIntelligence` luôn chủ động hành quân thẳng về trung tâm nơi GM đứng và giao chiến với các Party phòng thủ.
