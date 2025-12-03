using System;
using System.Collections.Generic;
using System.IO;

class Chess
{
    static string[,] board = new string[8, 8];
    static string turn = "white";

    static List<string> moveHistory = new List<string>();
    static bool[] castlingRights = new bool[6];
    static int epTargetCol = -1;
    static int epTargetRow = -1;



    static void getCoords(string sqr, out int colNo, out int rowNo)
    {
        char column = sqr[0];
        char row = sqr[1];
        colNo = column - 'a';
        rowNo = 7 - (row - '1');
    }

    static string getSquareName(int col, int row)
    {
        char c = (char)('a' + col);
        char r = (char)('8' - row);
        return "" + c + r;
    }

    static bool isValidSquare(int col, int row)
    {
        return col >= 0 && col <= 7 && row >= 0 && row <= 7;
    }

    static bool isValidSquareStr(string sqr)
    {
        if (sqr == null || sqr.Length != 2) return false;
        getCoords(sqr, out int c, out int r);
        return isValidSquare(c, r);
    }

    static string getPieceAt(int col, int row)
    {
        if (!isValidSquare(col, row)) return null;
        return board[col, row];
    }

    static void setPieceAt(int col, int row, string piece)
    {
        board[col, row] = piece;
    }

    static void initializeBoard()
    {
        for (int i = 0; i < 8; i++) for (int j = 0; j < 8; j++) board[i, j] = null;
        for (int i = 0; i < 6; i++) castlingRights[i] = false;
        moveHistory.Clear();
        epTargetCol = -1; epTargetRow = -1;
        turn = "white";

        string[] backRankBlack = { "bR", "bN", "bB", "bQ", "bK", "bB", "bN", "bR" };
        for (int i = 0; i < 8; i++)
        {
            board[i, 0] = backRankBlack[i];
            board[i, 1] = "bP";
        }

        string[] backRankWhite = { "wR", "wN", "wB", "wQ", "wK", "wB", "wN", "wR" };
        for (int i = 0; i < 8; i++)
        {
            board[i, 6] = "wP";
            board[i, 7] = backRankWhite[i];
        }
    }



    static bool IsSquareAttacked(int col, int row, string attackerColor)
    {
        int pawnDir = (attackerColor == "white") ? -1 : 1;
        int pRow = (attackerColor == "white") ? row + 1 : row - 1;
        if (isValidSquare(col - 1, pRow)) { string p = getPieceAt(col - 1, pRow); if (p == attackerColor[0] + "P") return true; }
        if (isValidSquare(col + 1, pRow)) { string p = getPieceAt(col + 1, pRow); if (p == attackerColor[0] + "P") return true; }

        int[] knX = { 1, 2, 2, 1, -1, -2, -2, -1 };
        int[] knY = { -2, -1, 1, 2, 2, 1, -1, -2 };
        for (int k = 0; k < 8; k++)
        {
            if (isValidSquare(col + knX[k], row + knY[k]))
            {
                string p = getPieceAt(col + knX[k], row + knY[k]);
                if (p == attackerColor[0] + "N") return true;
            }
        }

        for (int i = -1; i <= 1; i++) for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;
                if (isValidSquare(col + i, row + j))
                {
                    string p = getPieceAt(col + i, row + j);
                    if (p == attackerColor[0] + "K") return true;
                }
            }

        int[] dx = { 0, 0, 1, -1 }; int[] dy = { 1, -1, 0, 0 };
        for (int d = 0; d < 4; d++)
        {
            for (int k = 1; k < 8; k++)
            {
                int nx = col + dx[d] * k, ny = row + dy[d] * k;
                if (!isValidSquare(nx, ny)) break;
                string p = getPieceAt(nx, ny);
                if (p != null)
                {
                    if (p[0] == attackerColor[0] && (p[1] == 'R' || p[1] == 'Q')) return true;
                    break;
                }
            }
        }
        int[] bx = { 1, 1, -1, -1 }; int[] by = { 1, -1, 1, -1 };
        for (int d = 0; d < 4; d++)
        {
            for (int k = 1; k < 8; k++)
            {
                int nx = col + bx[d] * k, ny = row + by[d] * k;
                if (!isValidSquare(nx, ny)) break;
                string p = getPieceAt(nx, ny);
                if (p != null)
                {
                    if (p[0] == attackerColor[0] && (p[1] == 'B' || p[1] == 'Q')) return true;
                    break;
                }
            }
        }
        return false;
    }

    static bool IsInCheck(string color)
    {
        int kC = -1, kR = -1;
        char c = color[0];
        for (int i = 0; i < 8; i++) for (int j = 0; j < 8; j++)
            {
                if (board[i, j] == c + "K") { kC = i; kR = j; break; }
            }
        if (kC == -1) return false;
        string enemyColor = (color == "white") ? "black" : "white";
        return IsSquareAttacked(kC, kR, enemyColor);
    }

    static bool CanMoveGeometry(string piece, int c1, int r1, int c2, int r2, bool captureOnly = false)
    {
        char type = piece[1];
        char color = piece[0];
        int dCol = Math.Abs(c2 - c1);
        int dRow = Math.Abs(r2 - r1);

        string targetPiece = getPieceAt(c2, r2);

        if (targetPiece != null && targetPiece[0] == color) return false;

        if (type == 'P')
        {
            int dir = (color == 'w') ? -1 : 1;
            int startRow = (color == 'w') ? 6 : 1;

            if (dCol == 0 && targetPiece == null && !captureOnly)
            {
                if (r2 - r1 == dir) return true;
                if (r1 == startRow && r2 - r1 == 2 * dir && getPieceAt(c1, r1 + dir) == null) return true;
            }
            else if (dCol == 1 && r2 - r1 == dir)
            {
                if (targetPiece != null) return true;
                if (c2 == epTargetCol && r2 == epTargetRow) return true;
            }
            return false;
        }
        if (type == 'N') return (dCol == 1 && dRow == 2) || (dCol == 2 && dRow == 1);
        if (type == 'K') return dCol <= 1 && dRow <= 1;

        int stepX = Math.Sign(c2 - c1);
        int stepY = Math.Sign(r2 - r1);

        if (type == 'R') { if (dCol != 0 && dRow != 0) return false; }
        else if (type == 'B') { if (dCol != dRow) return false; }
        else if (type == 'Q') { if (!(dCol == 0 || dRow == 0 || dCol == dRow)) return false; }

        int curX = c1 + stepX, curY = r1 + stepY;
        while (curX != c2 || curY != r2)
        {
            if (getPieceAt(curX, curY) != null) return false;
            curX += stepX; curY += stepY;
        }
        return true;
    }

    static bool IsMoveLegal(int c1, int r1, int c2, int r2)
    {
        string piece = board[c1, r1];
        string target = board[c2, r2];
        string color = (piece[0] == 'w') ? "white" : "black";

        board[c2, r2] = piece;
        board[c1, r1] = null;

        string epCaptured = null;
        if (piece[1] == 'P' && c2 == epTargetCol && r2 == epTargetRow && c1 != c2)
        {
            int captureRow = r1;
            epCaptured = board[c2, captureRow];
            board[c2, captureRow] = null;
        }

        bool legal = !IsInCheck(color);

        board[c1, r1] = piece;
        board[c2, r2] = target;
        if (epCaptured != null)
        {
            int captureRow = r1;
            board[c2, captureRow] = epCaptured;
        }

        return legal;
    }

    static bool AnyLegalMoveExists(string color)
    {
        char c = color[0];
        for (int x = 0; x < 8; x++) for (int y = 0; y < 8; y++)
            {
                string p = board[x, y];
                if (p != null && p[0] == c)
                {
                    for (int tx = 0; tx < 8; tx++) for (int ty = 0; ty < 8; ty++)
                        {
                            if (CanMoveGeometry(p, x, y, tx, ty))
                            {
                                if (IsMoveLegal(x, y, tx, ty)) return true;
                            }
                        }
                }
            }
        return false;
    }

    static void SaveGame()
    {
        try
        {
            File.WriteAllLines("saved_game.txt", moveHistory);
            Console.WriteLine("Oyun 'saved_game.txt' dosyasına kaydedildi.");
        }
        catch (Exception ex) { Console.WriteLine("Kaydetme hatası: " + ex.Message); }
    }

    static void LoadSavedGame()
    {
        if (!File.Exists("saved_game.txt"))
        {
            Console.WriteLine("Kaydedilmiş oyun bulunamadı (saved_game.txt yok).");
            Console.WriteLine("Menüye dönmek için bir tuşa basın...");
            Console.ReadKey();
            return;
        }

        try
        {
            string[] moves = File.ReadAllLines("saved_game.txt");
            initializeBoard();
            Console.WriteLine("Kayıtlı oyun yükleniyor, lütfen bekleyin...");

            foreach (string move in moves)
            {
                if (!string.IsNullOrWhiteSpace(move))
                {
                    bool success = ParseAndMove(move, silent: true);
                    if (!success)
                    {
                        Console.WriteLine($"HATA: Kayıt dosyasındaki '{move}' hamlesi geçersiz veya bozuk.");
                        Console.ReadKey();
                        return;
                    }
                }
            }
            Console.WriteLine("Oyun başarıyla yüklendi!");
            RunPlayMode();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Yükleme hatası: " + ex.Message);
            Console.ReadKey();
        }
    }

    static void ShowHints()
    {
        Console.WriteLine("\n--- TÜM OLASI HAMLELER (HINT) ---");
        char turnChar = turn[0];
        bool foundAny = false;

        for (int c = 0; c < 8; c++)
        {
            for (int r = 0; r < 8; r++)
            {
                string p = board[c, r];
                if (p != null && p[0] == turnChar)
                {
                    for (int tc = 0; tc < 8; tc++)
                    {
                        for (int tr = 0; tr < 8; tr++)
                        {
                            if (CanMoveGeometry(p, c, r, tc, tr) && IsMoveLegal(c, r, tc, tr))
                            {
                                string targetP = board[tc, tr];
                                bool isCapture = (targetP != null);
                                if (p[1] == 'P' && tc == epTargetCol && tr == epTargetRow) isCapture = true;

                                string note = isCapture ? " (YİYOR)" : "";
                                Console.WriteLine($"- {p} {getSquareName(c, r)} -> {getSquareName(tc, tr)}{note}");
                                foundAny = true;
                            }
                        }
                    }
                }
            }
        }
        if (!foundAny) Console.WriteLine("Geçerli hamle bulunamadı.");
        Console.WriteLine("------------------------------------------");
    }


    static bool ParseAndMove(string input, bool silent = false)
    {

        input = input.Replace("+", "").Replace("#", "");

        if (input == "O-O" || input == "0-0")
        {
            if (HandleCastling(shortCastle: true, silent)) { moveHistory.Add(input); return true; }
            return false;
        }
        if (input == "O-O-O" || input == "0-0-0")
        {
            if (HandleCastling(shortCastle: false, silent)) { moveHistory.Add(input); return true; }
            return false;
        }

        char promotionPiece = ' ';
        string cleanInput = input;


        char lastChar = cleanInput.Length > 0 ? cleanInput[cleanInput.Length - 1] : ' ';
        if (lastChar == 'Q' || lastChar == 'R' || lastChar == 'B' || lastChar == 'N')
        {
            promotionPiece = lastChar;
            cleanInput = cleanInput.Substring(0, cleanInput.Length - 1);
        }

        if (cleanInput.Length < 2) return false;
        string targetStr = cleanInput.Substring(cleanInput.Length - 2);
        if (!isValidSquareStr(targetStr)) return false;

        getCoords(targetStr, out int toCol, out int toRow);

        char pieceType = 'P';
        if (cleanInput[0] >= 'A' && cleanInput[0] <= 'Z') pieceType = cleanInput[0];

        List<int[]> candidates = new List<int[]>();
        char turnChar = (turn == "white") ? 'w' : 'b';

        for (int c = 0; c < 8; c++) for (int r = 0; r < 8; r++)
            {
                string p = board[c, r];
                if (p != null && p[0] == turnChar && p[1] == pieceType)
                {
                    if (CanMoveGeometry(p, c, r, toCol, toRow))
                    {
                        if (IsMoveLegal(c, r, toCol, toRow))
                        {
                            bool matchesHint = true;
                            if (pieceType == 'P' && cleanInput.Contains("x"))
                            {
                                if (cleanInput[0] != (char)('a' + c)) matchesHint = false;
                            }
                            else if (pieceType != 'P' && cleanInput.Length > 3)
                            {
                                char hint = cleanInput[1];
                                if (hint >= 'a' && hint <= 'h') { if (c != hint - 'a') matchesHint = false; }
                                else if (hint >= '1' && hint <= '8') { if (r != 7 - (hint - '1')) matchesHint = false; }
                            }
                            if (matchesHint) candidates.Add(new int[] { c, r });
                        }
                    }
                }
            }

        if (candidates.Count == 0) { if (!silent) Console.WriteLine("Hatalı hamle!"); return false; }
        if (candidates.Count > 1) { if (!silent) Console.WriteLine("Belirsiz hamle!"); return false; }

        int fromCol = candidates[0][0];
        int fromRow = candidates[0][1];
        string movingPiece = board[fromCol, fromRow];

        if (pieceType == 'P' && toCol == epTargetCol && toRow == epTargetRow && toCol != fromCol)
        {
            int capturedPawnRow = (turn == "white") ? toRow + 1 : toRow - 1;
            board[toCol, capturedPawnRow] = null;
        }

        board[toCol, toRow] = movingPiece;
        board[fromCol, fromRow] = null;

        if (pieceType == 'P' && (toRow == 0 || toRow == 7))
        {
            char promo = (promotionPiece != ' ') ? promotionPiece : 'Q';
            board[toCol, toRow] = turnChar.ToString() + promo;
        }

        UpdateCastlingRights(movingPiece, fromCol, fromRow);

        epTargetCol = -1; epTargetRow = -1;
        if (pieceType == 'P' && Math.Abs(toRow - fromRow) == 2)
        {
            epTargetCol = fromCol;
            epTargetRow = (fromRow + toRow) / 2;
        }

        moveHistory.Add(input);
        turn = (turn == "white") ? "black" : "white";

        if (!AnyLegalMoveExists(turn))
        {
            if (!silent)
            {
                if (IsInCheck(turn)) Console.WriteLine("ŞAH MAT! Oyun bitti.");
                else Console.WriteLine("PAT! (Berabere)");
            }
        }
        else if (IsInCheck(turn))
        {
            if (!silent) Console.WriteLine("ŞAH!");
        }

        return true;
    }

    static bool HandleCastling(bool shortCastle, bool silent = false)
    {
        string color = turn;
        int row = (color == "white") ? 7 : 0;
        char c = (color == "white") ? 'w' : 'b';

        bool kMoved = (color == "white") ? castlingRights[0] : castlingRights[3];
        bool rMoved = (shortCastle) ?
            ((color == "white") ? castlingRights[2] : castlingRights[5]) :
            ((color == "white") ? castlingRights[1] : castlingRights[4]);

        if (kMoved || rMoved) { if (!silent) Console.WriteLine("Rok hakkı yok."); return false; }

        int kCol = 4;
        int rCol = (shortCastle) ? 7 : 0;
        int step = (shortCastle) ? 1 : -1;

        for (int i = kCol + step; i != rCol; i += step)
        {
            if (board[i, row] != null) { if (!silent) Console.WriteLine("Rok yolu kapalı."); return false; }
        }

        if (IsSquareAttacked(kCol, row, (color == "white" ? "black" : "white"))) { if (!silent) Console.WriteLine("Şah çekilirken rok yapılmaz."); return false; }
        if (IsSquareAttacked(kCol + step, row, (color == "white" ? "black" : "white"))) return false;
        if (IsSquareAttacked(kCol + 2 * step, row, (color == "white" ? "black" : "white"))) return false;

        board[kCol, row] = null;
        board[rCol, row] = null;
        board[kCol + 2 * step, row] = c + "K";
        board[kCol + step, row] = c + "R";

        UpdateCastlingRights(c + "K", kCol, row);

        turn = (turn == "white") ? "black" : "white";
        return true;
    }

    static void UpdateCastlingRights(string piece, int fromCol, int fromRow)
    {
        if (piece == "wK") { castlingRights[0] = true; }
        if (piece == "bK") { castlingRights[3] = true; }

        if (piece == "wR")
        {
            if (fromCol == 0 && fromRow == 7) castlingRights[1] = true;
            if (fromCol == 7 && fromRow == 7) castlingRights[2] = true;
        }
        if (piece == "bR")
        {
            if (fromCol == 0 && fromRow == 0) castlingRights[4] = true;
            if (fromCol == 7 && fromRow == 0) castlingRights[5] = true;
        }
    }



    static char GetPieceSymbol(char type)
    {
        return type;
    }

    static void printBoard()
    {
        Console.Clear();
        Console.WriteLine("\n    a  b  c  d  e  f  g  h");
        Console.Write("  +");
        for (int k = 0; k < 24; k++) Console.Write("-");
        Console.WriteLine("+");

        for (int i = 0; i < 8; i++)
        {
            Console.ResetColor();
            Console.Write((8 - i) + " |");

            for (int j = 0; j < 8; j++)
            {
                if ((i + j) % 2 == 0)
                    Console.BackgroundColor = ConsoleColor.Gray;
                else
                    Console.BackgroundColor = ConsoleColor.DarkGray;

                string piece = board[j, i];
                string symbol = "   ";

                if (piece != null)
                {
                    if (piece[0] == 'w') Console.ForegroundColor = ConsoleColor.Cyan;
                    else Console.ForegroundColor = ConsoleColor.Red;

                    symbol = " " + GetPieceSymbol(piece[1]) + " ";
                }

                Console.Write(symbol);
            }

            Console.ResetColor();
            Console.WriteLine("| " + (8 - i));
        }

        Console.WriteLine("  +------------------------+");
        Console.WriteLine("    a  b  c  d  e  f  g  h");

        Console.ResetColor();
        Console.WriteLine("\nSIRA: " + (turn == "white" ? "BEYAZ (Mavi)" : "SİYAH (Kırmızı)"));
        if (IsInCheck(turn)) Console.WriteLine("DURUM: ŞAH!");
        Console.WriteLine("\nKomutlar: 'H'=İpucu, 'save'=Kaydet, 'exit'=Çıkış");
    }

    static void CreateSampleGameFile()
    {

        File.Delete("game.txt");
        if (!File.Exists("game.txt"))
        {
            string[] moves = {
                "e4", "e5", "Nf3", "d6", "d4", "Bg4", "dxe5", "Bxf3", "Qxf3", "dxe5",
                "Bc4", "Nf6", "Qb3", "Qe7", "Nc3", "c6", "Bg5", "b5", "Nxb5", "cxb5",
                "Bxb5+", "Nbd7", "O-O-O", "Rd8", "Rxd7", "Rxd7", "Rd1", "Qe6", "Bxd7+",
                "Nxd7", "Qb8+", "Nxb8", "Rd8#"
            };
            File.WriteAllLines("game.txt", moves);
        }
    }

    static void RunPlayMode()
    {
        while (true)
        {
            printBoard();
            Console.WriteLine("Hamle gir (e4, Nf3) veya komut (H, save, exit):");
            Console.Write("> ");

            string input = Console.ReadLine();
            if (string.IsNullOrEmpty(input)) continue;

            if (input == "exit") break;

            if (input == "H" || input == "h")
            {
                ShowHints();
                Console.WriteLine("\nDevam etmek için tuşa bas...");
                Console.ReadKey();
                continue;
            }

            if (input == "save")
            {
                SaveGame();
                Console.WriteLine("\nDevam etmek için tuşa bas...");
                Console.ReadKey();
                continue;
            }

            bool success = ParseAndMove(input);
            if (!success)
            {
                Console.WriteLine("Geçersiz hamle! Tuşa bas...");
                Console.ReadKey();
            }
        }
    }

    static void RunDemoMode()
    {
        if (!File.Exists("game.txt")) { Console.WriteLine("Dosya yok."); return; }
        string[] moves = File.ReadAllLines("game.txt");
        int idx = 0;

        while (idx < moves.Length)
        {
            printBoard();
            Console.WriteLine("\nDEMO (Morphy's Opera Game): Sonraki -> " + moves[idx]);
            Console.WriteLine("[SPACE] Oynat, [ENTER] Devral, [ESC] Çık");

            var keyInfo = Console.ReadKey(true);

            if (keyInfo.Key == ConsoleKey.Spacebar)
            {
                ParseAndMove(moves[idx]);
                idx++;
            }
            else if (keyInfo.Key == ConsoleKey.Enter)
            {
                Console.WriteLine("Oyun devralınıyor...");
                System.Threading.Thread.Sleep(500);
                RunPlayMode();
                return;
            }
            else if (keyInfo.Key == ConsoleKey.Escape) return;
        }
        printBoard(); Console.WriteLine("Demo bitti."); Console.ReadKey();
    }

    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        CreateSampleGameFile();
        initializeBoard();
        while (true)
        {
            Console.Clear();
            Console.WriteLine("DEHSET SATRANC");
            Console.WriteLine("1. Oyna (Play Mode)");
            Console.WriteLine("2. Demo (game.txt Oku)");
            Console.WriteLine("3. Kayıtlı Oyundan Devam Et");
            Console.WriteLine("4. Çıkış");
            string c = Console.ReadLine();
            if (c == "1") RunPlayMode();
            else if (c == "2") { initializeBoard(); RunDemoMode(); }
            else if (c == "3") { LoadSavedGame(); }
            else if (c == "4") break;
        }
    }
}
