namespace Conde.Markov;

/// <summary>
/// Uma CADEIA DE MARKOV: uma matriz em que cada linha e uma distribuicao de
/// probabilidade.
///
/// A linha i diz para onde ir a partir do estado i, e e so isso. Toda a teoria
/// sai dessa regra, e as coisas que ela NAO exige sao o assunto mais
/// interessante: a matriz nao precisa ser simetrica, nem irredutivel, nem
/// aperiodica, e cada uma dessas faltas quebra uma coisa diferente.
///
/// As transicoes sao guardadas em FRACAO, e nao em ponto flutuante. Uma cadeia
/// com 1/3 nas linhas tem estacionaria exata, e so da para dizer que o metodo
/// iterativo chegou na resposta certa se existir uma resposta certa.
/// </summary>
public sealed class Cadeia
{
    public Cadeia(string nome, Fracao[,] transicoes)
    {
        ArgumentNullException.ThrowIfNull(transicoes);

        var n = transicoes.GetLength(0);
        if (transicoes.GetLength(1) != n) throw new ArgumentException("a matriz precisa ser quadrada", nameof(transicoes));

        for (var i = 0; i < n; i++)
        {
            var soma = Fracao.Zero;
            for (var j = 0; j < n; j++)
            {
                if (transicoes[i, j].Sinal < 0)
                    throw new ArgumentException($"probabilidade negativa em ({i}, {j})", nameof(transicoes));
                soma += transicoes[i, j];
            }

            // A soma e conferida em FRACAO, entao ela e exata. Em ponto
            // flutuante, uma linha de tres tercos somaria 0,9999999999999999 e
            // a conferencia precisaria de tolerancia, que e justamente o que
            // esconde linha errada de verdade.
            if (soma != Fracao.Um) throw new ArgumentException($"a linha {i} soma {soma}, e nao um", nameof(transicoes));
        }

        Nome = nome;
        Transicoes = transicoes;
        Estados = n;
    }

    public string Nome { get; }

    public Fracao[,] Transicoes { get; }

    public int Estados { get; }

    public override string ToString() => Nome;

    /// <summary>A matriz em ponto flutuante, para os metodos iterativos.</summary>
    public double[,] EmDouble()
    {
        var m = new double[Estados, Estados];
        for (var i = 0; i < Estados; i++)
            for (var j = 0; j < Estados; j++)
                m[i, j] = Transicoes[i, j].EmDouble();

        return m;
    }

    /// <summary>
    /// Se da para ir de qualquer estado a qualquer outro, em algum numero de
    /// passos: IRREDUTIVEL.
    ///
    /// Sem isso a cadeia tem mais de uma distribuicao estacionaria, e "a"
    /// estacionaria deixa de existir no singular. O PageRank existe para
    /// consertar exatamente essa falta na web.
    /// </summary>
    public bool Irredutivel()
    {
        for (var inicio = 0; inicio < Estados; inicio++)
        {
            var visto = new bool[Estados];
            var fila = new Queue<int>();
            visto[inicio] = true;
            fila.Enqueue(inicio);

            while (fila.Count > 0)
            {
                var atual = fila.Dequeue();
                for (var j = 0; j < Estados; j++)
                    if (!visto[j] && !Transicoes[atual, j].EhZero) { visto[j] = true; fila.Enqueue(j); }
            }

            if (visto.Any(v => !v)) return false;
        }

        return true;
    }

    /// <summary>
    /// O PERIODO de um estado: o maior divisor comum dos tamanhos dos ciclos
    /// que voltam para ele.
    ///
    /// Periodo maior que um e o caso em que a cadeia tem estacionaria e MESMO
    /// ASSIM a iteracao de potencia nunca converge: ela fica oscilando entre as
    /// fases para sempre.
    /// </summary>
    public int Periodo(int estado = 0)
    {
        var distancia = new int[Estados];
        Array.Fill(distancia, -1);

        distancia[estado] = 0;
        var fila = new Queue<int>();
        fila.Enqueue(estado);
        var periodo = 0;

        while (fila.Count > 0)
        {
            var atual = fila.Dequeue();

            for (var j = 0; j < Estados; j++)
            {
                if (Transicoes[atual, j].EhZero) continue;

                if (distancia[j] < 0) { distancia[j] = distancia[atual] + 1; fila.Enqueue(j); }
                else periodo = Mdc(periodo, distancia[atual] + 1 - distancia[j]);
            }
        }

        return periodo == 0 ? 0 : Math.Abs(periodo);
    }

    public bool Aperiodica() => Periodo() == 1;

    private static int Mdc(int a, int b)
    {
        a = Math.Abs(a);
        b = Math.Abs(b);
        while (b != 0) (a, b) = (b, a % b);
        return a;
    }

    /// <summary>Monta uma cadeia a partir de numeradores sobre um denominador comum.</summary>
    public static Cadeia De(string nome, int denominador, params int[][] linhas)
    {
        ArgumentNullException.ThrowIfNull(linhas);

        var n = linhas.Length;
        var m = new Fracao[n, n];

        for (var i = 0; i < n; i++)
            for (var j = 0; j < n; j++)
                m[i, j] = new Fracao(linhas[i][j], denominador);

        return new Cadeia(nome, m);
    }

    /// <summary>
    /// A PREVISAO DO TEMPO de livro: sol e chuva, com memoria de um dia.
    ///
    /// A estacionaria dela tem resposta fechada e conhecida, entao ela e o
    /// primeiro teste de qualquer metodo daqui.
    /// </summary>
    public static Cadeia Tempo() => De("tempo", 10, [9, 1], [5, 5]);

    /// <summary>
    /// Um PASSEIO EM CICLO de quatro estados, andando sempre para frente.
    ///
    /// Ele e irredutivel, tem estacionaria uniforme e tem PERIODO QUATRO: a
    /// iteracao de potencia nele nunca converge, e esse e o contraexemplo mais
    /// util do repositorio.
    /// </summary>
    public static Cadeia Ciclo(int estados = 4)
    {
        var m = new Fracao[estados, estados];
        for (var i = 0; i < estados; i++)
            for (var j = 0; j < estados; j++)
                m[i, j] = j == (i + 1) % estados ? Fracao.Um : Fracao.Zero;

        return new Cadeia($"ciclo de {estados}", m);
    }

    /// <summary>
    /// Duas ilhas que nao se falam: a cadeia REDUTIVEL.
    ///
    /// Ela tem infinitas distribuicoes estacionarias, uma para cada mistura das
    /// duas ilhas, e e o caso em que falar em "a" estacionaria e erro.
    /// </summary>
    public static Cadeia DuasIlhas() => De("duas ilhas", 4, [3, 1, 0, 0], [1, 3, 0, 0], [0, 0, 2, 2], [0, 0, 1, 3]);

    /// <summary>
    /// Um passeio em LINHA com paredes nas pontas, que e o caso lento de
    /// proposito: para atravessar, o passeio precisa de muitos passos, e o tempo
    /// de mistura cresce com o quadrado do tamanho.
    /// </summary>
    public static Cadeia Linha(int estados = 5)
    {
        var m = new Fracao[estados, estados];
        var meio = new Fracao(1, 2);

        for (var i = 0; i < estados; i++)
        {
            if (i == 0) { m[0, 0] = meio; m[0, 1] = meio; }
            else if (i == estados - 1) { m[i, i] = meio; m[i, i - 1] = meio; }
            else { m[i, i - 1] = meio; m[i, i + 1] = meio; }
        }

        return new Cadeia($"linha de {estados}", m);
    }

    /// <summary>
    /// Uma cadeia com um estado PEGAJOSO: ele quase nunca solta.
    ///
    /// Ela e irredutivel e aperiodica, entao converge, e converge devagar: o
    /// segundo autovalor fica perto de um e a iteracao de potencia precisa de
    /// centenas de passos.
    /// </summary>
    public static Cadeia Pegajosa() => De("pegajosa", 1000, [999, 1], [1, 999]);

    public static IEnumerable<Cadeia> Todas()
    {
        yield return Tempo();
        yield return Linha();
        yield return Pegajosa();
        yield return Ciclo();
        yield return DuasIlhas();
    }

    /// <summary>As que convergem: irredutiveis e aperiodicas.</summary>
    public static IEnumerable<Cadeia> Boas() => Todas().Where(c => c.Irredutivel() && c.Aperiodica());
}
