using System.Collections.Generic;

// Tipos de edificio y su botín
public enum LootPool { Casa, Oficina, Tienda, Hospital, Joyeria }

// Crear objetos desde la tabla
public static class ItemFactory
{
    private struct Entry
    {
        public string name;
        public int value;
        public float weight;

        public Entry(string name, int value, float weight)
        {
            this.name = name;
            this.value = value;
            this.weight = weight;
        }
    }

    private static readonly Dictionary<LootPool, Entry[]> pools = new Dictionary<LootPool, Entry[]>
    {
        {
            LootPool.Casa, new[]
            {
                new Entry("Televisor",           600, 12f),
                new Entry("Joyas",              1800,  0.3f),
                new Entry("Reloj de oro",        900,  0.2f),
                new Entry("Radio antigua",       250,  2f),
                new Entry("Cubiertos de plata",  500,  1.5f),
                new Entry("Cuadro",              700,  3.5f),
            }
        },
        {
            LootPool.Oficina, new[]
            {
                new Entry("Laptop",                1200, 2f),
                new Entry("Monitor",                400, 5f),
                new Entry("Impresora",              300, 9f),
                new Entry("Caja fuerte chica",      700, 20f),
                new Entry("Tablet",                 450, 0.6f),
                new Entry("Documentos de empresa",  800, 1f),
            }
        },
        {
            LootPool.Tienda, new[]
            {
                new Entry("Dinero de caja",      700,  1f),
                new Entry("Licores",             250,  1.5f),
                new Entry("Consola",             400,  3f),
                new Entry("Cajas de mercaderia", 400, 11f),
                new Entry("Celulares",           600,  0.4f),
                new Entry("Ropa de marca",       300,  2f),
            }
        },
        {
            LootPool.Hospital, new[]
            {
                new Entry("Equipo medico portatil",  1600, 6.5f),
                new Entry("Desfibrilador portatil",  2500, 4f),
                new Entry("Instrumental quirurgico", 1300, 2f),
                new Entry("Monitor de signos",       1450, 6.5f),
                new Entry("Medicamentos",            1100, 2.5f),
                new Entry("Cajas de insumos",         750, 9f),
            }
        },
        {
            LootPool.Joyeria, new[]
            {
                new Entry("Anillo de oro",    2000, 0.2f),
                new Entry("Reloj suizo",      4000, 0.3f),
                new Entry("Collar de perlas", 3000, 0.35f),
                new Entry("Diamantes",        5500, 0.075f),
                new Entry("Lingote de plata", 2350, 3f),
                new Entry("Caja de joyas",    3250, 2.25f),
            }
        }
    };

    public static Item Create(System.Random rng, LootPool pool)
    {
        Entry[] list = pools[pool];
        Entry e = list[rng.Next(list.Length)];
        return new Item(e.name, e.value, e.weight);
    }
}
