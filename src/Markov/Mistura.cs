namespace Conde.Markov;

/// <summary>
/// O TEMPO DE MISTURA: quantos passos ate a cadeia esquecer de onde comecou.
///
/// A definicao padrao e o primeiro passo em que a distancia de variacao total
/// ate a estacionaria cai abaixo de 1/4, no PIOR ponto de partida. O pior
/// importa: a media esconde o caso que manda, e numa linha com paredes o pior
/// comeco e uma das pontas.
///
/// O numero que governa isso e o segundo autovalor, e a relacao com ele e
/// grosseira dos dois lados: o tempo de mistura e maior que (1/(1-lambda) - 1)
/// vezes um logaritmo e menor que outra conta parecida. A medida mostra onde o
/// numero realmente cai.
/// </summary>
public static class Mistura
{
    /// <summary>
    /// O tempo de mistura a partir de um ponto de partida, para um limiar dado.
    /// </summary>
    public static int Tempo(Cadeia cadeia, int inicio, double limiar = 0.25, int limite = 1_000_000)
    {
        ArgumentNullException.ThrowIfNull(cadeia);

        var alvo = Exata.EstacionariaEmDouble(cadeia);
        if (alvo is null) return limite;

        var matriz = cadeia.EmDouble();
        var atual = Potencia.Concentrada(cadeia.Estados, inicio);

        for (var passo = 0; passo < limite; passo++)
        {
            if (Exata.Variacao(atual, alvo) < limiar) return passo;
            atual = Potencia.UmPasso(matriz, atual);
        }

        return limite;
    }

    /// <summary>
    /// O tempo de mistura de verdade: o PIOR entre todos os pontos de partida.
    /// </summary>
    public static int Pior(Cadeia cadeia, double limiar = 0.25, int limite = 1_000_000)
    {
        ArgumentNullException.ThrowIfNull(cadeia);

        var pior = 0;
        for (var inicio = 0; inicio < cadeia.Estados; inicio++)
            pior = Math.Max(pior, Tempo(cadeia, inicio, limiar, limite));

        return pior;
    }

    /// <summary>
    /// O TEMPO DE RELAXAMENTO, 1/(1 - lambda), que e a escala de tempo que o
    /// segundo autovalor dita.
    /// </summary>
    public static double Relaxamento(double segundoAutovalor) =>
        segundoAutovalor >= 1 ? double.PositiveInfinity : 1 / (1 - segundoAutovalor);

    /// <summary>
    /// Quantos passos para a distancia cair abaixo de um valor dado, a partir do
    /// pior comeco. E a mesma conta do tempo de mistura com outro limiar, e ela
    /// existe para a medida mostrar que o custo cresce com o LOGARITMO da
    /// exigencia.
    /// </summary>
    public static int PassosAte(Cadeia cadeia, double limiar, int limite = 1_000_000) =>
        Pior(cadeia, limiar, limite);

    /// <summary>
    /// A cadeia PREGUICOSA: com probabilidade 1/2 ela fica onde esta, e com 1/2
    /// ela da o passo que daria.
    ///
    /// Essa e a cura padrao para periodicidade, e ela e quase gratis: os
    /// autovalores viram (1 + lambda)/2, entao o segundo autovalor sobe e a
    /// cadeia fica mais LENTA, mas o que era menos um, que impedia a
    /// convergencia, vira zero.
    /// </summary>
    public static Cadeia Preguicosa(Cadeia cadeia)
    {
        ArgumentNullException.ThrowIfNull(cadeia);

        var n = cadeia.Estados;
        var m = new Fracao[n, n];
        var meio = new Fracao(1, 2);

        for (var i = 0; i < n; i++)
            for (var j = 0; j < n; j++)
                m[i, j] = meio * cadeia.Transicoes[i, j] + (i == j ? meio : Fracao.Zero);

        return new Cadeia($"{cadeia.Nome} preguicosa", m);
    }
}
