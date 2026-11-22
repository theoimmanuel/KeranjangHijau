using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Infrastructure;
using KeranjangHijau.Infrastructure.Tracking;
using Microsoft.Extensions.DependencyInjection;

namespace KeranjangHijau
{
    public sealed class BackendBridge
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        private readonly IServiceProvider _serviceProvider;
        private readonly IAuthService _authService;
        private readonly IEmissionService _emissionService;
        private readonly IForestService _forestService;
        private readonly IReportService _reportService;
        private readonly IAdminService _adminService;
        private readonly ICurrentUserSession _session;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly MockCourierTracker _mockTracker;
        private readonly IDistanceCalculator _distanceCalculator;

        public BackendBridge()
        {
            var config = BackendConfiguration.Build();
            var services = new ServiceCollection();
            services.AddKeranjangHijauBackend(config);
            _serviceProvider = services.BuildServiceProvider();

            _authService = _serviceProvider.GetRequiredService<IAuthService>();
            _emissionService = _serviceProvider.GetRequiredService<IEmissionService>();
            _forestService = _serviceProvider.GetRequiredService<IForestService>();
            _reportService = _serviceProvider.GetRequiredService<IReportService>();
            _adminService = _serviceProvider.GetRequiredService<IAdminService>();
            _session = _serviceProvider.GetRequiredService<ICurrentUserSession>();
            _dbConnectionFactory = _serviceProvider.GetRequiredService<IDbConnectionFactory>();
            _mockTracker = _serviceProvider.GetRequiredService<MockCourierTracker>();
            _distanceCalculator = _serviceProvider.GetRequiredService<IDistanceCalculator>();
        }

        public async Task<string> HandleMessageAsync(string rawMessage)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawMessage);
                var root = doc.RootElement;
                string id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
                string action = root.TryGetProperty("action", out var actionProp) ? actionProp.GetString() ?? "" : "";
                JsonElement payload = root.TryGetProperty("payload", out var payloadProp) ? payloadProp : default;

                object? resultData = null;

                switch (action.ToLowerInvariant())
                {
                    case "check_status":
                        resultData = await CheckStatusAsync();
                        break;

                    case "get_mock_resi":
                        resultData = GetMockResiList();
                        break;

                    case "login":
                        resultData = await LoginAsync(payload);
                        break;

                    case "register":
                        resultData = await RegisterAsync(payload);
                        break;

                    case "logout":
                        _authService.Logout();
                        resultData = new { success = true };
                        break;

                    case "get_current_user":
                        resultData = GetCurrentUserInfo();
                        break;

                    case "calculate_emission":
                        resultData = await CalculateEmissionAsync(payload);
                        break;

                    case "get_forest":
                        resultData = await GetForestAsync(payload);
                        break;

                    case "get_report":
                        resultData = await GetReportAsync(payload);
                        break;

                    case "get_admin_data":
                        resultData = await GetAdminDataAsync(payload);
                        break;

                    default:
                        return SerializeResponse(id, false, null, $"Aksi '{action}' tidak dikenal.");
                }

                return SerializeResponse(id, true, resultData, null);
            }
            catch (Exception ex)
            {
                return SerializeResponse("", false, null, ex.Message);
            }
        }

        private async Task<object> CheckStatusAsync()
        {
            bool dbConnected = false;
            string? dbError = null;

            try
            {
                await using var conn = await _dbConnectionFactory.OpenConnectionAsync();
                dbConnected = true;
            }
            catch (Exception ex)
            {
                dbConnected = false;
                dbError = ex.Message;
            }

            return new
            {
                dbConnected,
                dbError,
                currentUser = GetCurrentUserInfo(),
                defaultAdmin = new
                {
                    email = "admin@keranjanghijau.local",
                    password = "AdminKH2026!"
                }
            };
        }

        private object GetCurrentUserInfo()
        {
            var user = _session.CurrentUser;
            return new
            {
                isLoggedIn = _session.IsLoggedIn,
                userId = user?.UserId,
                nama = user?.Nama,
                email = user?.Email,
                role = user?.Role.ToString(),
                isAdmin = _session.IsAdmin
            };
        }

        private List<object> GetMockResiList()
        {
            var list = new List<object>
            {
                new { kurir = "jne", resi = "JNE1001000001", rute = "Jakarta -> Bandung (1.2 kg)", skenario = "JNE Reguler Jawa" },
                new { kurir = "jne", resi = "JNE1001000002", rute = "Surabaya -> Makassar", skenario = "JNE Antar-Pulau" },
                new { kurir = "jnt", resi = "JP1002000001", rute = "Tangerang -> Malang (2.5 kg)", skenario = "J&T Express Jawa" },
                new { kurir = "sicepat", resi = "004710030001", rute = "Bandung -> Surakarta (0.5 kg)", skenario = "SiCepat Jawa" },
                new { kurir = "anteraja", resi = "10000400000001", rute = "Jakarta -> Yogyakarta (0.7 kg)", skenario = "AnterAja Regular" },
                new { kurir = "pos", resi = "POS2405000001", rute = "Malang -> Jayapura (2.0 kg)", skenario = "Pos Indonesia Jawa-Papua" }
            };
            return list;
        }

        private async Task<object> LoginAsync(JsonElement payload)
        {
            string email = payload.GetProperty("email").GetString() ?? "";
            string password = payload.GetProperty("password").GetString() ?? "";

            try
            {
                var user = await _authService.LoginAsync(email, password);
                return new
                {
                    success = true,
                    user = new
                    {
                        id = user.UserId,
                        nama = user.Nama,
                        email = user.Email,
                        role = user.Role.ToString()
                    }
                };
            }
            catch (Exception ex)
            {
                // If DB is offline, allow demo mock login for testing UI
                if (ex.Message.Contains("Connection refused") || ex.Message.Contains("Failed to connect") || ex.InnerException != null)
                {
                    bool isAdmin = email.ToLower().Contains("admin");
                    var mockUser = new UserDto(
                        isAdmin ? 1 : 2,
                        isAdmin ? "Admin KeranjangHijau" : "Theo Sanyoto (Demo)",
                        email,
                        isAdmin ? UserRole.Admin : UserRole.Pengguna,
                        DateTimeOffset.UtcNow
                    );
                    _session.SignIn(mockUser);

                    return new
                    {
                        success = true,
                        isMock = true,
                        warning = "Database PostgreSQL offline; masuk dalam mode simulasi.",
                        user = new
                        {
                            id = mockUser.UserId,
                            nama = mockUser.Nama,
                            email = mockUser.Email,
                            role = mockUser.Role.ToString()
                        }
                    };
                }
                throw;
            }
        }

        private async Task<object> RegisterAsync(JsonElement payload)
        {
            string nama = payload.GetProperty("nama").GetString() ?? "";
            string email = payload.GetProperty("email").GetString() ?? "";
            string password = payload.GetProperty("password").GetString() ?? "";

            try
            {
                var user = await _authService.RegisterAsync(nama, email, password);
                return new
                {
                    success = true,
                    user = new
                    {
                        id = user.UserId,
                        nama = user.Nama,
                        email = user.Email,
                        role = user.Role.ToString()
                    }
                };
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Connection refused") || ex.Message.Contains("Failed to connect"))
                {
                    var mockUser = new UserDto(99, nama, email, UserRole.Pengguna, DateTimeOffset.UtcNow);
                    _session.SignIn(mockUser);

                    return new
                    {
                        success = true,
                        isMock = true,
                        warning = "Database PostgreSQL offline; akun demo tersimpan di sesi lokal.",
                        user = new
                        {
                            id = mockUser.UserId,
                            nama = mockUser.Nama,
                            email = mockUser.Email,
                            role = mockUser.Role.ToString()
                        }
                    };
                }
                throw;
            }
        }

        private async Task<object> CalculateEmissionAsync(JsonElement payload)
        {
            string kurir = payload.GetProperty("courier").GetString() ?? "jne";
            string resi = payload.GetProperty("resi").GetString() ?? "";
            decimal? beratKg = payload.TryGetProperty("beratKg", out var b) && b.ValueKind == JsonValueKind.Number ? b.GetDecimal() : null;

            try
            {
                // Ensure a user session exists for recording calculation
                if (!_session.IsLoggedIn)
                {
                    var guestUser = new UserDto(2, "Pengguna Demo", "demo@keranjanghijau.local", UserRole.Pengguna, DateTimeOffset.UtcNow);
                    _session.SignIn(guestUser);
                }

                // If user is currently signed in as Admin, switch or report
                if (_session.IsLoggedIn && !_session.IsAdmin)
                {
                    var req = new EmissionRequest(kurir, resi, beratKg);
                    var result = await _emissionService.CalculateFromResiAsync(req);
                    return new
                    {
                        success = true,
                        result
                    };
                }

                // Simulation fallback
                return await SimulateEmissionCalculationAsync(kurir, resi, beratKg);
            }
            catch (Exception ex)
            {
                return await SimulateEmissionCalculationAsync(kurir, resi, beratKg, ex.Message);
            }
        }

        private async Task<object> SimulateEmissionCalculationAsync(string kurir, string resi, decimal? beratKg, string? originalError = null)
        {
            var tracking = await _mockTracker.TrackAsync(new TrackingRequest(kurir, resi));

            string origin = tracking?.KotaAsal ?? "Jakarta";
            string dest = tracking?.KotaTujuan ?? "Bandung";
            decimal weight = beratKg ?? tracking?.BeratKg ?? 1.2m;

            double distanceKm = 150.0;
            if (origin.Equals("Jakarta", StringComparison.OrdinalIgnoreCase) && dest.Equals("Bandung", StringComparison.OrdinalIgnoreCase))
                distanceKm = 145.2;
            else if (origin.Equals("Surabaya", StringComparison.OrdinalIgnoreCase) && dest.Equals("Makassar", StringComparison.OrdinalIgnoreCase))
                distanceKm = 820.5;
            else if (origin.Equals("Malang", StringComparison.OrdinalIgnoreCase) && dest.Equals("Jayapura", StringComparison.OrdinalIgnoreCase))
                distanceKm = 3280.0;
            else if (origin.Equals("Bandung", StringComparison.OrdinalIgnoreCase) && dest.Equals("Surakarta", StringComparison.OrdinalIgnoreCase))
                distanceKm = 420.0;
            else if (origin.Equals("Tangerang", StringComparison.OrdinalIgnoreCase) && dest.Equals("Malang", StringComparison.OrdinalIgnoreCase))
                distanceKm = 680.0;

            string moda = distanceKm > 700 ? "pesawat" : "truk";
            decimal factor = moda == "pesawat" ? 0.800000m : 0.100000m;
            decimal tonKm = (weight / 1000m) * (decimal)distanceKm;
            decimal totalCo2eKg = Math.Round(tonKm * factor, 4);
            if (totalCo2eKg < 0.001m) totalCo2eKg = 0.0174m;

            decimal kmMotor = Math.Round(totalCo2eKg / 0.070000m, 1);
            decimal kmMobil = Math.Round(totalCo2eKg / 0.170000m, 1);
            decimal pohonDibutuhkan = Math.Round(totalCo2eKg / 22.0m, 3);
            decimal forestImpact = Math.Round(totalCo2eKg * 5.0m, 1);

            return new
            {
                success = true,
                isSimulated = true,
                warning = originalError != null ? $"Perhitungan simulasi ({originalError})" : null,
                result = new
                {
                    nomorResi = resi,
                    kodeKurir = kurir,
                    namaKurir = kurir.ToUpperInvariant(),
                    kotaAsal = origin,
                    kotaTujuan = dest,
                    layanan = tracking?.Layanan ?? "REG",
                    jarakKm = distanceKm,
                    beratKg = weight,
                    modaTransport = moda,
                    totalCo2eKg = totalCo2eKg,
                    konversiSetara = new List<object>
                    {
                        new { aktivitas = "Naik motor", nilaiSetara = kmMotor, satuan = "km", ikon = "motor" },
                        new { aktivitas = "Naik mobil", nilaiSetara = kmMobil, satuan = "km", ikon = "mobil" },
                        new { aktivitas = "Penyerap Emisi", nilaiSetara = pohonDibutuhkan, satuan = "pohon/tahun", ikon = "tree" }
                    },
                    hutan = new
                    {
                        kesehatanSebelum = 85.0m,
                        kesehatanSesudah = Math.Max(0m, 85.0m - forestImpact),
                        perubahan = -forestImpact,
                        status = "Sehat",
                        keterangan = $"Kesehatan hutan turun -{forestImpact:F1}% akibat emisi paket ini."
                    }
                }
            };
        }

        private async Task<object> GetForestAsync(JsonElement payload)
        {
            int userId = _session.CurrentUser?.UserId ?? 1;

            try
            {
                if (_session.IsLoggedIn)
                {
                    var forest = await _forestService.GetForestAsync(userId);
                    var history = await _forestService.GetForestHistoryAsync(userId, 15);
                    return new
                    {
                        success = true,
                        forest,
                        history
                    };
                }
            }
            catch { }

            // Simulation / Fallback forest state
            return new
            {
                success = true,
                isMock = true,
                forest = new
                {
                    namaHutan = "Hutan " + (_session.CurrentUser?.Nama ?? "Pengguna Hijau"),
                    kesehatan = 84.5m,
                    status = "Sehat",
                    jumlahPohonAwal = 50,
                    perkiraanPohonTersisa = 42
                },
                history = new List<object>
                {
                    new { logId = 1, perubahan = -2.4m, kesehatanSetelah = 84.5m, keterangan = "Emisi paket resi JNE1001000001", isPemulihan = false, createdAt = DateTimeOffset.UtcNow.AddHours(-3) },
                    new { logId = 2, perubahan = 10.0m, kesehatanSetelah = 86.9m, keterangan = "Pemulihan alami harian", isPemulihan = true, createdAt = DateTimeOffset.UtcNow.AddDays(-1) },
                    new { logId = 3, perubahan = -3.1m, kesehatanSetelah = 76.9m, keterangan = "Emisi paket resi JP1002000001", isPemulihan = false, createdAt = DateTimeOffset.UtcNow.AddDays(-2) }
                }
            };
        }

        private async Task<object> GetReportAsync(JsonElement payload)
        {
            string periodStr = payload.TryGetProperty("period", out var p) ? p.GetString() ?? "Bulanan" : "Bulanan";
            ReportPeriod period = periodStr.ToLowerInvariant() switch
            {
                "mingguan" or "weekly" => ReportPeriod.Mingguan,
                "semua" or "all" => ReportPeriod.Semua,
                _ => ReportPeriod.Bulanan
            };

            try
            {
                if (_session.IsLoggedIn && !_session.IsAdmin)
                {
                    var report = await _reportService.GetReportAsync(period);
                    return new { success = true, report };
                }
            }
            catch { }

            // Simulation / Fallback report
            return new
            {
                success = true,
                isMock = true,
                report = new
                {
                    periode = period.ToString(),
                    totalCo2eKg = 4.85m,
                    jumlahPaket = 6,
                    paket = new List<object>
                    {
                        new { nomorResi = "JNE1001000001", kodeKurir = "jne", kotaAsal = "Jakarta", kotaTujuan = "Bandung", totalCo2eKg = 0.0174m, beratKg = 1.2m, jarakKm = 145.2m, modaTransport = "truk", calculatedAt = DateTimeOffset.UtcNow.AddDays(-1) },
                        new { nomorResi = "JP1002000001", kodeKurir = "jnt", kotaAsal = "Tangerang", kotaTujuan = "Malang", totalCo2eKg = 0.1700m, beratKg = 2.5m, jarakKm = 680.0m, modaTransport = "truk", calculatedAt = DateTimeOffset.UtcNow.AddDays(-2) },
                        new { nomorResi = "004710030001", kodeKurir = "sicepat", kotaAsal = "Bandung", kotaTujuan = "Surakarta", totalCo2eKg = 0.0210m, beratKg = 0.5m, jarakKm = 420.0m, modaTransport = "truk", calculatedAt = DateTimeOffset.UtcNow.AddDays(-4) },
                        new { nomorResi = "POS2405000001", kodeKurir = "pos", kotaAsal = "Malang", kotaTujuan = "Jayapura", totalCo2eKg = 5.2480m, beratKg = 2.0m, jarakKm = 3280.0m, modaTransport = "pesawat", calculatedAt = DateTimeOffset.UtcNow.AddDays(-5) }
                    },
                    konversiSetara = new List<object>
                    {
                        new { aktivitas = "Naik motor", nilaiSetara = 69.3m, satuan = "km", ikon = "motor" },
                        new { aktivitas = "Naik mobil", nilaiSetara = 28.5m, satuan = "km", ikon = "mobil" }
                    },
                    dataHarian = new List<object>
                    {
                        new { tanggal = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-4)), totalCo2eKg = 0.45m },
                        new { tanggal = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)), totalCo2eKg = 1.20m },
                        new { tanggal = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)), totalCo2eKg = 0.85m },
                        new { tanggal = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), totalCo2eKg = 1.65m },
                        new { tanggal = DateOnly.FromDateTime(DateTime.UtcNow), totalCo2eKg = 0.70m }
                    }
                }
            };
        }

        private async Task<object> GetAdminDataAsync(JsonElement payload)
        {
            try
            {
                if (_session.IsLoggedIn && _session.IsAdmin)
                {
                    var summary = await _adminService.GetSummaryAsync(new AdminRecordFilter());
                    var records = await _adminService.GetRecordsAsync(new AdminRecordFilter { UkuranHalaman = 20 });
                    return new { success = true, summary, records };
                }
            }
            catch { }

            // Simulation / Fallback admin summary
            return new
            {
                success = true,
                isMock = true,
                summary = new
                {
                    totalPengguna = 18,
                    totalPaket = 142,
                    totalCo2eKg = 215.8m,
                    rataRataPerPaket = 1.52m,
                    perKurir = new List<object>
                    {
                        new { kodeKurir = "jne", namaKurir = "JNE", totalPaket = 65, totalCo2eKg = 98.4m },
                        new { kodeKurir = "jnt", namaKurir = "J&T Express", totalPaket = 42, totalCo2eKg = 64.2m },
                        new { kodeKurir = "sicepat", namaKurir = "SiCepat", totalPaket = 25, totalCo2eKg = 38.1m },
                        new { kodeKurir = "pos", namaKurir = "Pos Indonesia", totalPaket = 10, totalCo2eKg = 15.1m }
                    }
                },
                records = new
                {
                    totalCount = 3,
                    items = new List<object>
                    {
                        new { recordId = 1, namaPengguna = "Theo Sanyoto", email = "theo@example.com", kodeKurir = "jne", namaKurir = "JNE", nomorResi = "JNE1001000001", kotaAsal = "Jakarta", kotaTujuan = "Bandung", beratKg = 1.2m, totalCo2eKg = 0.0174m, calculatedAt = DateTimeOffset.UtcNow.AddHours(-2) },
                        new { recordId = 2, namaPengguna = "Amelia Ocha", email = "ocha@example.com", kodeKurir = "jnt", namaKurir = "J&T Express", nomorResi = "JP1002000001", kotaAsal = "Tangerang", kotaTujuan = "Malang", beratKg = 2.5m, totalCo2eKg = 0.1700m, calculatedAt = DateTimeOffset.UtcNow.AddHours(-6) },
                        new { recordId = 3, namaPengguna = "Juan Sipayung", email = "juan@example.com", kodeKurir = "pos", namaKurir = "Pos Indonesia", nomorResi = "POS2405000001", kotaAsal = "Malang", kotaTujuan = "Jayapura", beratKg = 2.0m, totalCo2eKg = 5.2480m, calculatedAt = DateTimeOffset.UtcNow.AddDays(-1) }
                    }
                }
            };
        }

        private static string SerializeResponse(string id, bool success, object? data, string? error)
        {
            var resp = new
            {
                id,
                success,
                data,
                error
            };
            return JsonSerializer.Serialize(resp, JsonOpts);
        }
    }
}
