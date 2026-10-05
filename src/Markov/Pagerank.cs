namespace Conde.Markov;

/// <summary>
/// O PAGERANK, de 1998: uma cadeia de Markov com dois remendos.
///
/// A ideia e um passeio aleatorio pelos links. Ela nao funciona crua por duas
/// razoes concretas, e os dois remendos sao exatamente elas:
///
/// 1. Paginas SEM LINK DE SAIDA nao tem linha de probabilidade: a matriz nem e
///    estocastica. O remendo e sortear uma pagina qualquer a partir delas.
///
/// 2. A web nao e irredutiva nem aperiodica, entao a estacionaria nao e unica e
///    a iteracao pode nao convergir. O remendo e o AMORTECIMENTO: com
///    probabilidade 0,85 segue um link, com 0,15 pula para uma pagina sorteada.
///
/// O segundo remendo nao e so conserto: ele e o que da a VELOCIDADE. A cadeia
/// amortecida tem segundo autovalor no maximo igual ao amortecimento, entao a
/// convergencia encolhe por 0,85 a cada passo em qualquer grafo, por pior que o
/// grafo seja. A medida confere isso num grafo construido para ser o pior
/// possivel.
/// </summary>
public static class Pagerank
{
    /// <summary>
    /// Monta a cadeia do PageRank a partir dos links, com os dois remendos.
    ///
    /// O amortecimento entra como fracao para a cadeia continuar exata: com
    /// 85/100 a estacionaria tem denominador inteiro, e ela e o juiz da
    /// iteracao.
    /// </summary>
    public static Cadeia Montar(bool[,] links, Fracao amortecimento)
    {
        ArgumentNullException.ThrowIfNull(links);

        var n = links.GetLength(0);
        var m = new Fracao[n, n];
        var espalhado = new Fracao(1, n);
        var pulo = Fracao.Um - amortecimento;

        for (var i = 0; i < n; i++)
        {
            var saidas = 0;
            for (var j = 0; j < n; j++) if (links[i, j]) saidas++;

            for (var j = 0; j < n; j++)
            {
                // A pagina sem saida sorteia qualquer uma: sem isso a linha
                // soma zero e a matriz nem e uma cadeia.
                var seguindo = saidas == 0 ? espalhado : (links[i, j] ? new Fracao(1, saidas) : Fracao.Zero);
                m[i, j] = amortecimento * seguindo + pulo * espalhado;
            }
        }

        return new Cadeia($"pagerank {amortecimento}", m);
    }

    public static Cadeia Montar(bool[,] links, int cima = 85, int baixo = 100) =>
        Montar(links, new Fracao(cima, baixo));

    /// <summary>
    /// Um grafo em CORRENTE: cada pagina aponta so para a seguinte, e a ultima
    /// nao aponta para ninguem.
    ///
    /// Ele e o pior caso honesto: redutivel, com estado sem saida, e com o
    /// caminho mais longo possivel entre duas paginas.
    /// </summary>
    public static bool[,] Corrente(int paginas)
    {
        var links = new bool[paginas, paginas];
        for (var i = 0; i + 1 < paginas; i++) links[i, i + 1] = true;
        return links;
    }

    /// <summary>
    /// Uma ESTRELA: todas apontam para a primeira, e a primeira nao aponta para
    /// ninguem. E o grafo em que o PageRank deveria dar a resposta mais obvia
    /// possivel.
    /// </summary>
    public static bool[,] Estrela(int paginas)
    {
        var links = new bool[paginas, paginas];
        for (var i = 1; i < paginas; i++) links[i, 0] = true;
        return links;
    }

    /// <summary>
    /// Duas ilhas com UM link entre elas, que e o grafo em que o amortecimento
    /// mais muda a resposta.
    /// </summary>
    public static bool[,] Ilhas(int porIlha)
    {
        var n = porIlha * 2;
        var links = new bool[n, n];

        for (var i = 0; i < porIlha; i++)
            for (var j = 0; j < porIlha; j++)
                if (i != j) { links[i, j] = true; links[porIlha + i, porIlha + j] = true; }

        links[porIlha - 1, porIlha] = true;
        return links;
    }

    /// <summary>
    /// A velocidade da iteracao, medida: a razao com que a distancia ate a
    /// resposta exata encolhe a cada passo.
    ///
    /// A teoria diz que ela e no maximo o amortecimento. A medida diz onde ela
    /// realmente fica.
    /// </summary>
    public static double RazaoMedida(bool[,] links, int cima = 85, int baixo = 100, int passos = 60) =>
        Potencia.SegundoAutovalorMedido(Montar(links, cima, baixo), passos);
}
