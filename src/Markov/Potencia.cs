namespace Conde.Markov;

/// <summary>
/// A ITERACAO DE POTENCIA: multiplicar a distribuicao pela matriz, de novo e de
/// novo, ate parar de mudar.
///
/// E o metodo que todo mundo usa, inclusive o PageRank original, e ele e uma
/// linha. O que ele NAO faz e o assunto: ele converge quando a cadeia e
/// irredutivel e APERIODICA, e no ciclo de quatro estados, que tem estacionaria
/// uniforme perfeitamente bem definida, ele oscila para sempre sem chegar em
/// lugar nenhum.
///
/// A velocidade dele tambem nao e livre: a distancia ate a estacionaria encolhe
/// pelo SEGUNDO maior autovalor em modulo a cada passo. Com ele em 0,998, como
/// na cadeia pegajosa, sao milhares de passos para um ganho que na cadeia do
/// tempo custa dez.
/// </summary>
public static class Potencia
{
    /// <summary>Um passo da cadeia em ponto flutuante.</summary>
    public static double[] UmPasso(double[,] matriz, double[] distribuicao)
    {
        ArgumentNullException.ThrowIfNull(matriz);
        ArgumentNullException.ThrowIfNull(distribuicao);

        var n = distribuicao.Length;
        var depois = new double[n];

        for (var j = 0; j < n; j++)
        {
            var soma = 0.0;
            for (var i = 0; i < n; i++) soma += distribuicao[i] * matriz[i, j];
            depois[j] = soma;
        }

        return depois;
    }

    /// <summary>A distribuicao que comeca toda num estado so.</summary>
    public static double[] Concentrada(int estados, int onde = 0)
    {
        var d = new double[estados];
        d[onde] = 1;
        return d;
    }

    /// <summary>
    /// Itera ate a mudanca ficar abaixo da tolerancia, devolvendo quantos passos
    /// custou.
    ///
    /// O criterio e "parou de mudar", que e o criterio que todo mundo usa e que
    /// NAO e o mesmo que "chegou na resposta". A medida compara os dois: a
    /// distancia ate a estacionaria exata no momento em que o metodo decidiu
    /// parar.
    /// </summary>
    public static (double[] Distribuicao, int Passos, bool Parou) Iterar(
        Cadeia cadeia, double[]? inicio = null, double tolerancia = 1e-12, int limite = 1_000_000)
    {
        ArgumentNullException.ThrowIfNull(cadeia);

        var matriz = cadeia.EmDouble();
        var atual = inicio is null ? Concentrada(cadeia.Estados) : (double[])inicio.Clone();

        for (var passo = 1; passo <= limite; passo++)
        {
            var proximo = UmPasso(matriz, atual);
            var mudanca = Exata.Variacao(proximo, atual);
            atual = proximo;

            if (mudanca <= tolerancia) return (atual, passo, true);
        }

        return (atual, limite, false);
    }

    /// <summary>
    /// A distancia ate a estacionaria exata a cada passo, que e a medida de
    /// verdade.
    /// </summary>
    public static List<double> Distancias(Cadeia cadeia, int passos, double[]? inicio = null)
    {
        ArgumentNullException.ThrowIfNull(cadeia);

        var alvo = Exata.EstacionariaEmDouble(cadeia);
        if (alvo is null) return [];

        var matriz = cadeia.EmDouble();
        var atual = inicio is null ? Concentrada(cadeia.Estados) : (double[])inicio.Clone();
        var trilha = new List<double> { Exata.Variacao(atual, alvo) };

        for (var i = 0; i < passos; i++)
        {
            atual = UmPasso(matriz, atual);
            trilha.Add(Exata.Variacao(atual, alvo));
        }

        return trilha;
    }

    /// <summary>
    /// O SEGUNDO AUTOVALOR em modulo, medido pela razao com que a distancia
    /// encolhe.
    ///
    /// Ele nao e calculado: e observado. A distancia ate a estacionaria encolhe
    /// por esse fator a cada passo, entao a razao entre duas distancias
    /// consecutivas, longe do comeco, E o numero.
    /// </summary>
    /// <param name="piso">
    /// A distancia abaixo da qual a razao deixa de significar o autovalor.
    ///
    /// Esse piso me corrigiu. Com ele em 1e-13, a cadeia do tempo media 0,40045
    /// em vez de 0,4: a essa altura a distancia ja e tao pequena que o proprio
    /// arredondamento do ponto flutuante domina a diferenca, e a razao passa a
    /// medir ruido. Com o piso em 1e-10 ela bate com a formula fechada ate a
    /// nona casa.
    /// </param>
    public static double SegundoAutovalorMedido(Cadeia cadeia, int passos = 200, double piso = 1e-10)
    {
        var trilha = Distancias(cadeia, passos);
        if (trilha.Count < 3) return double.NaN;

        for (var i = trilha.Count - 1; i > 0; i--)
            if (trilha[i] > piso && trilha[i - 1] > piso) return trilha[i] / trilha[i - 1];

        return double.NaN;
    }

    /// <summary>
    /// Para a cadeia de DOIS estados, o segundo autovalor tem formula fechada:
    /// 1 - p - q, com p e q as probabilidades de trocar de estado.
    ///
    /// Isso da a comparacao exata que a medida precisa, e ela cobre a cadeia do
    /// tempo e a pegajosa.
    /// </summary>
    public static Fracao SegundoAutovalorExato(Cadeia cadeia)
    {
        ArgumentNullException.ThrowIfNull(cadeia);
        if (cadeia.Estados != 2) throw new ArgumentException("a formula fechada so vale para dois estados", nameof(cadeia));

        return Fracao.Um - cadeia.Transicoes[0, 1] - cadeia.Transicoes[1, 0];
    }
}
