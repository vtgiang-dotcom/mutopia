# BÁO CÁO TOÁN HỌC & HIỆU CHUẨN: TIẾN TRÌNH RESET VÀ KINH TẾ OPENMU S6E3

> **Phiên bản:** 2.1 — Calibrated & Analytically Verified  
> **Căn cứ mã nguồn:** OpenMU Season 6 Episode 3 Core Logic  
> **Mã nguồn trích xuất:**
> * Công thức EXP nhân vật: [`GameContext.cs:L27`](file:///d:/Project/mu/OpenMU-master/src/GameLogic/GameContext.cs#L27)
> * Công thức EXP quái: [`AttackableExtensions.cs:L596-L618`](file:///d:/Project/mu/OpenMU-master/src/GameLogic/AttackableExtensions.cs#L596-L618)
> * Cơ chế phân phối Zen: [`MoneyDistribution.cs:L148-L159`](file:///d:/Project/mu/OpenMU-master/src/GameLogic/MoneyDistribution.cs#L148-L159)
> * Cơ chế Reset & Trần điểm: [`ResetCharacterAction.cs:L130-L198`](file:///d:/Project/mu/OpenMU-master/src/GameLogic/Resets/ResetCharacterAction.cs#L130-L198)

---

## 1. BẢNG HIỆU CHUẨN EXP QUÁI (MONSTER EXP CALIBRATION)

Trong OpenMU, kinh nghiệm nhận được khi tiêu diệt quái vật không cố định mà tuân theo hàm phi tuyến tính theo cấp độ quái $L_m$:

$$\text{BaseEXP}(L_m) = \begin{cases} 
\frac{(L_m + 25) \cdot L_m}{3} \times 1.25 & \text{khi } L_m < 65 \\ 
\left[ \frac{(L_m + 25) \cdot L_m}{3} + (L_m - 64) \cdot \lfloor \frac{L_m}{4} \rfloor \right] \times 1.25 & \text{khi } L_m \ge 65 
\end{cases}$$

### Bảng tham chiếu dữ liệu trích xuất từ Code:

| Dải Level | Quái vật đại diện & Bản đồ | Level quái ($L_m$) | EXP Gốc chuẩn (`BaseExp`) | Kills / Giờ | EXP / Giờ (tại 1×) |
| :---: | :--- | :---: | :---: | :---: | :---: |
| **1 – 30** | Bull Fighter *(Lorencia/Noria)* | 12 | **185** | 900 | $166,500$ |
| **31 – 60** | Elite Yeti *(Devias/Dungeon 1)* | 36 | **915** | 850 | $777,750$ |
| **61 – 100** | Devil *(Dungeon 3/LostTower 1-3)* | 52 | **1,668** | 800 | $1,334,400$ |
| **101 – 150**| Death Knight *(LostTower 7/Atlans 2)*| 72 | **3,090** | 750 | $2,317,500$ |
| **151 – 200**| Bloody Wolf *(Atlans 3/Tarkan 1)* | 84 | **4,340** | 700 | $3,038,000$ |
| **201 – 260**| Iron Wheel *(Tarkan 2/Icarus)* | 102 | **6,585** | 650 | $4,280,250$ |
| **261 – 320**| Splinter Wolf *(Aida 2/Kanturu 1)* | 118 | **8,988** | 580 | $5,213,040$ |
| **321 – 360**| Persona *(Kanturu Relics/Karutan 1)*| 138 | **12,518** | 500 | $6,259,000$ |
| **361 – 400**| Condor *(Karutan 2/Raklion/Swamp)* | 156 | **16,250** | 420 | $6,825,000$ |

---

## 2. BASELINE 1× THẬT: KỊCH BẢN SCENARIO A (LEVEL 1 $\rightarrow$ 400)

* **Tổng EXP tích lũy (Lv 1 $\rightarrow$ 400):** **$3,822,148,080\text{ EXP}$** ($3.822$ tỷ).
* **Tổng số quái cần giết tại 1×:** **$310,466\text{ kills}$**.
* **Thời gian cày lý thuyết thuần túy (100% cắm bãi):** **$629.6\text{ giờ}$** ($\approx 26.2\text{ ngày}$ cắm 24/24).
* **Thời gian trải nghiệm thực tế:**
  Do người chơi phải di chuyển về thành mua máu/mana, sửa trang bị, bị PK, tranh bãi farm và tử vong (hiệu suất thời gian thực tế $\approx 50\% - 60\%$), tổng thời gian thực tế để max cấp 400 tại Rate 1× là:
  $$T_{\text{thực tế}} = \frac{629.6\text{h}}{0.50 \sim 0.60} \approx \mathbf{1.050\text{ giờ} \sim 1.260\text{ giờ}}$$
  *(Xác nhận: Hoàn toàn trùng khớp với kinh nghiệm thực chiến máy chủ MU Webzen cổ điển).*

---

## 3. CÔNG THỨC GIẢI TÍCH: TỔNG EXP 36 RESET & CHỌN RATE THEO THỜI LƯỢNG

### 3.1. Tổng EXP cần cày cho 36 Chu kỳ Reset Bậc Thang
Mỗi chu kỳ $k$ ($k = 1 \rightarrow 36$) cày từ Level 10 đến Level $L_{\text{target}} = 50 + (k-1) \times 10$:

$$\text{TotalExp}_{36} = \sum_{k=1}^{36} \Big(\text{ExpTable}\big[50 + (k-1) \times 10\big] - \text{ExpTable}[10]\Big) = \mathbf{20,139,543,000\text{ EXP}} \approx \mathbf{20.14 \times 10^9\text{ EXP}}$$

### 3.2. Công thức tính thời lượng tổng quát $T$ (giờ)

$$T(\text{hours}) = \frac{20.14 \times 10^9}{\overline{\text{KillsPerHour}} \times \overline{\text{BaseExp}} \times \text{Rate}}$$

Do hơn $85\%$ lượng EXP nằm ở các map cấp cao (Kanturu, Karutan, Raklion, Swamp) với $\overline{\text{BaseExp}} \approx 13,800$ và $\overline{\text{KillsPerHour}} \approx 460$:

$$\overline{\text{KillsPerHour}} \times \overline{\text{BaseExp}} \approx 6.35 \times 10^6\text{ EXP/giờ tại 1x}$$

$$\implies T(\text{hours}) \approx \frac{20.14 \times 10^9}{6.35 \times 10^6 \times \text{Rate}} \approx \mathbf{\frac{3,170}{\text{Rate}}}$$

$$\iff \mathbf{\text{Rate EXP}} \approx \mathbf{\frac{3,170}{T(\text{hours})}}$$

---

## 4. QUY TẮC CUNG CẦU NGỌC: $0.1\% \leftrightarrow \text{RATE EXP}$

### 4.1. Nhu cầu Ngọc (Jewel Demand) cho 1 Nhân vật
* Nâng cấp Set đồ lên $+11$ và xoay Wing 2: $\approx \mathbf{56.5\text{ viên}}$ (Bless/Soul/Chaos/Life).
* Nộp phí Reset bậc thang (các mốc cao): $\approx \mathbf{32.0\text{ viên}}$ (Chaos/Creation).
* **Tổng cầu tối thiểu:** **$88.5\text{ viên ngọc}$**.

### 4.2. Mô hình Toán học Nguồn Cung Ngọc từ Quái Thường
Với tỷ lệ rớt ngọc gốc $0.1\%$ ($0.001$) và hiệu suất nhặt $70\%$ ($0.70$):

$$\text{Số Ngọc Nhặt Được} = \text{Total Kills} \times 0.001 \times 0.70 = \left( \frac{20.14 \times 10^9}{13,800 \times \text{Rate}} \right) \times 0.0007 = \mathbf{\frac{1,021.6}{\text{Rate}}}$$

### 4.3. Bảng Kiểm Chứng Cân Bằng Cung Cầu Ngọc

| Rate EXP của Server | Tổng quái giết trong 36 Reset | Lượng Ngọc nhặt được (Drop $0.1\%$) | Tổng cầu ngọc | Đánh giá Cân Bằng Kinh Tế |
| :---: | :---: | :---: | :---: | :--- |
| **$400\text{x}$** *(Dynamic cũ)* | $3,648\text{ kills}$ | **$2.5\text{ viên}$** | $88.5\text{ viên}$ | ❌ **Khủng hoảng đói ngọc** (Thiếu 97% nhu cầu). |
| **$64\text{x}$** *(Server 50h)* | $22,803\text{ kills}$ | **$16.0\text{ viên}$** | $88.5\text{ viên}$ | ❌ **Thiếu hụt trầm trọng** (Thiếu 82% nhu cầu). |
| **$30\text{x}$** *(Server 100h)*| $48,647\text{ kills}$ | **$34.1\text{ viên}$** | $88.5\text{ viên}$ | ⚠️ Đủ xoay cánh, thiếu ngọc đập đồ. |
| **$16\text{x}$** *(Server 200h)*| $91,213\text{ kills}$ | **$63.9\text{ viên}$** | $88.5\text{ viên}$ | ⚠️ Vừa đủ nâng đồ cơ bản, không đủ trả phí reset. |
| **$11.5\text{x}$** | **$126,906\text{ kills}$** | **$88.5\text{ viên}$** | **$88.5\text{ viên}$** | ✅ **Điểm cân bằng tự nhiên tại Drop $0.1\%$**. |

---

## 5. CÔNG THỨC ĐIỀU CHỈNH TỶ LỆ RỚT NGỌC KHI TĂNG RATE EXP

Để ngăn chặn hoàn toàn "nạn đói ngọc" khi vận hành máy chủ ở các mức Rate EXP cao ($> 11.5\text{x}$), Admin chỉ cần áp dụng công thức điều chỉnh tỷ lệ rơi ngọc:

$$\mathbf{\text{JewelDropChance}} = 0.001 \times \frac{\text{Rate EXP}}{11.5}$$

### Bảng Cấu Hình Khuyến Nghị Chuẩn:

| Mục tiêu Thời lượng hoàn thành 36 Reset | Rate EXP Trung Bình ($\text{Rate} = \frac{3,170}{T}$) | Tỷ lệ Rớt Ngọc tương ứng (`JewelDropChance`) | Thời gian Casual (3h/ngày) | Thời gian Auto (16h/ngày) |
| :--- | :---: | :---: | :---: | :---: |
| **1. Server Siêu Nhanh (50 giờ)** | **$64\text{x}$** | **$0.55\%$** ($1/180$) | $16.7\text{ ngày}$ | $3.1\text{ ngày}$ |
| **2. Server Cân Bằng Chuẩn (100 giờ - Gold Preset)** | **$30\text{x}$** *(Bậc thang 50x $\rightarrow$ 5x)* | **$0.26\% \sim 0.30\%$** ($1/350$) | $33.3\text{ ngày}$ | $6.2\text{ ngày}$ |
| **3. Server Bền Vững Trung Hạn (200 giờ)** | **$16\text{x}$** *(Bậc thang 30x $\rightarrow$ 3x)* | **$0.14\%$** ($1/700$) | $66.7\text{ ngày}$ | $12.5\text{ ngày}$ |
| **4. Server Cày Cuốc Dài Hạn (500 giờ)** | **$6.3\text{x} \approx 6\text{x}$** | **$0.05\% \sim 0.10\%$** (Gốc) | $166.7\text{ ngày}$ | $31.2\text{ ngày}$ |

---

## 6. DANH SÁCH FILE KẾT QUẢ MÔ PHỎNG CHI TIẾT

Tất cả các tệp dữ liệu thực nghiệm phục vụ việc tra cứu đã được xuất tại:
* [`simulation/csv_results/monster_exp_calibration.csv`](file:///d:/Project/mu/simulation/csv_results/monster_exp_calibration.csv) — Bảng Base EXP của 9 dải quái vật.
* [`simulation/csv_results/scenario_a.csv`](file:///d:/Project/mu/simulation/csv_results/scenario_a.csv) — Kết quả chạy Baseline 1× chuẩn 629.6 giờ.
* [`simulation/csv_results/gold_preset_audited.csv`](file:///d:/Project/mu/simulation/csv_results/gold_preset_audited.csv) — Kết quả mô phỏng Gold Preset (107.6 giờ).
* [`simulation/csv_results/jewel_economy.csv`](file:///d:/Project/mu/simulation/csv_results/jewel_economy.csv) — Bảng cung cầu ngọc chi tiết từng lần reset.
* [`simulation/csv_results/scenario_f_timeline.csv`](file:///d:/Project/mu/simulation/csv_results/scenario_f_timeline.csv) — Timeline 90 ngày của 3 hồ sơ người chơi.
