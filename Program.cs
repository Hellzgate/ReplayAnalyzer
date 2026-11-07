using System.Text;

namespace ReplayAnalyzer;

/// <summary>
/// EDOPro 리플레이(.yrpX) 분석기 — 뼈대.
/// 현재 구현: 헤더 파싱 + 폴더 목록.
/// 다음 단계: LZMA 해제 → 이름/파라미터 → 메시지 스트림(자세한 내용은 AGENTS.md).
/// </summary>
internal static class Program
{
    private const string DefaultReplayDir = @"D:\edopro\replay";

    private const uint MagicYrp1 = 0x31707279; // "yrp1"
    private const uint MagicYrpX = 0x58707279; // "yrpX"

    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        if (args.Length == 0)
        {
            ListReplays(DefaultReplayDir);
            return 0;
        }

        if (args[0] is "-h" or "--help" or "/?")
        {
            PrintHelp();
            return 0;
        }

        var path = args[0];
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"파일 없음: {path}");
            return 2;
        }

        return ShowInfo(path);
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            ReplayAnalyzer — EDOPro .yrpX 분석기 (뼈대)

            사용법:
              ReplayAnalyzer                        리플레이 폴더 요약
              ReplayAnalyzer <파일.yrpX>            헤더 정보
              ReplayAnalyzer --help                 도움말

            구현 예정:
              --names        플레이어 이름
              --packets      메시지 통계
              --scan <폴더>  폴더 전체 일괄 분석 (한 프로세스)
              --json         JSON 출력
            """);
    }

    private static void ListReplays(string dir)
    {
        if (!Directory.Exists(dir))
        {
            Console.WriteLine($"리플레이 폴더 없음: {dir}");
            return;
        }

        var files = new DirectoryInfo(dir)
            .GetFiles("*.yrpX", SearchOption.TopDirectoryOnly)
            .OrderByDescending(f => f.LastWriteTime)
            .ToList();

        Console.WriteLine($"리플레이 폴더: {dir}");
        Console.WriteLine($"파일 수: {files.Count}");

        var infos = files.Select(f => (File: f, Header: TryReadHeader(f.FullName))).ToList();
        var compressed = infos.Count(x => x.Header is { Compressed: true });
        Console.WriteLine($"압축(LZMA): {compressed} / 비압축: {files.Count - compressed}");
        Console.WriteLine();

        Console.WriteLine($"{"날짜",-20} {"크기",10} {"flag",8} 압축  파일");
        foreach (var (file, header) in infos.Take(10))
        {
            var flag = header is null ? "-" : $"0x{header.Flags:x}";
            var comp = header is null ? "?" : header.Compressed ? "Y" : "N";
            Console.WriteLine($"{file.LastWriteTime:yyyy-MM-dd HH:mm:ss} {file.Length,10:N0} {flag,8} {comp,4}  {file.Name}");
        }

        if (files.Count > 10)
            Console.WriteLine($"... 외 {files.Count - 10}개");
    }

    private static int ShowInfo(string path)
    {
        var header = TryReadHeader(path);
        if (header is null)
        {
            Console.Error.WriteLine("리플레이 헤더를 읽지 못했습니다 (손상되었거나 리플레이 파일이 아님).");
            return 1;
        }

        var file = new FileInfo(path);
        Console.WriteLine($"파일   : {file.FullName}");
        Console.WriteLine($"크기   : {file.Length:N0} B   수정: {file.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"매직   : {header.MagicName} (0x{header.Id:x8})  버전: 0x{header.Version:x8}");
        Console.WriteLine($"flag   : 0x{header.Flags:x}  확장헤더: {(header.ExtendedHeader ? "Y" : "N")}  압축: {(header.Compressed ? "Y" : "N")}");
        Console.WriteLine($"데이터 : 헤더 {header.HeaderSize}B, 본문 {file.Length - header.HeaderSize:N0} B");

        if (header.Compressed)
            Console.WriteLine("압축 해제: 아직 미구현 (다음 단계 — SharpCompress Decoder, props 5B + datasize)");
        else
            Console.WriteLine("비압축: 스트림 파싱은 다음 단계");

        return 0;
    }

    private static Header? TryReadHeader(string path)
    {
        try
        {
            using var fs = File.OpenRead(path);
            var buf = new byte[72];
            var read = fs.Read(buf, 0, buf.Length);
            if (read < 32) return null;

            var id = BitConverter.ToUInt32(buf, 0);
            if (id is not (MagicYrp1 or MagicYrpX)) return null;

            var flags = BitConverter.ToUInt32(buf, 8);
            var extended = (flags & 0x200u) != 0 && read >= 72;
            return new Header
            {
                Id = id,
                Version = BitConverter.ToUInt32(buf, 4),
                Flags = flags,
                DataSize = BitConverter.ToUInt32(buf, 16),
                Compressed = (flags & 0x1u) != 0,
                ExtendedHeader = extended,
                HeaderSize = extended ? 72 : 32,
            };
        }
        catch
        {
            return null;
        }
    }

    private sealed class Header
    {
        public uint Id { get; init; }
        public uint Version { get; init; }
        public uint Flags { get; init; }
        public uint DataSize { get; init; }
        public bool Compressed { get; init; }
        public bool ExtendedHeader { get; init; }
        public int HeaderSize { get; init; }

        public string MagicName => Id == MagicYrp1 ? "yrp1" : Id == MagicYrpX ? "yrpX" : "?";
    }
}
