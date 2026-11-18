namespace Platybytes.Llave
{
    // Los 5 laberintos del GDD dibujados como texto: cada carácter es una celda de 1 x 1 unidades.
    // Se han comprobado para que siempre haya camino hasta la cerradura y hasta la estrella.
    public static class Laberintos
    {
        // #  pared fija        R  pared de rebote     S  pared pegajosa
        // M  pared móvil (se desliza en horizontal)    V  pared móvil (en vertical)
        // B  salida de la bola G  guardia quieto      P  guardia que patrulla
        // E  estrella          C  cerradura (meta)    .  suelo
        public static readonly string[][] Todos =
        {
            // Laberinto 1: paredes fijas y guardias quietos (tutorial implícito)
            new string[] {
                "#####################",
                "#B...#.......#......#",
                "#....#.......#..G..E#",
                "#....#...#...#......#",
                "#....#...#...####.###",
                "#........#..........#",
                "#....#...#..........#",
                "#....#...#####..#####",
                "#..G.#.......#......#",
                "#....#.......#.....C#",
                "#####################",
            },
            // Laberinto 2: más guardias y pasillos más estrechos
            new string[] {
                "#####################",
                "#B..#.....#.......E.#",
                "#...#.....#..####...#",
                "#...#..G..#..#..#.G.#",
                "#...####..#..#..#...#",
                "#.........#.....#...#",
                "####..#####..G..#...#",
                "#.....#.........#...#",
                "#..G..#..########...#",
                "#..................C#",
                "#####################",
            },
            // Laberinto 3: paredes de rebote y guardias que patrullan
            new string[] {
                "#####################",
                "#B.....R.....#.....C#",
                "#......R.....#......#",
                "#####..R..####..#####",
                "#E..#........R......#",
                "#...#..P.....R......#",
                "#..RRRR..#####..#####",
                "#........R..........#",
                "#..G.....#...P......#",
                "#...................#",
                "#####################",
            },
            // Laberinto 4: paredes pegajosas
            new string[] {
                "#####################",
                "#B..S......#.......E#",
                "#...S......#...G....#",
                "#...SSSS...#..SSSS..#",
                "#.......G..#........#",
                "#####..#####..####..#",
                "#.........S.....R...#",
                "#....P..........R...#",
                "#..######.S..####...#",
                "#.........S..G.....C#",
                "#####################",
            },
            // Laberinto 5: paredes móviles y todo combinado
            new string[] {
                "#####################",
                "#B....M....#.......E#",
                "#..........#...P....#",
                "#####...#######..####",
                "#...R.......V.......#",
                "#...R...P...V...G...#",
                "#...RRRR....V..######",
                "#.......SS..........#",
                "#..G....SS..#####...#",
                "#.......M...#......C#",
                "#####################",
            },
        };
    }
}
