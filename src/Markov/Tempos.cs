namespace Conde.Markov;

/// <summary>
/// TEMPOS DE CHEGADA E DE RETORNO, resolvidos em fracao.
///
/// "Quantos passos, em media, ate chegar no estado alvo partindo de cada
/// estado?" e um sistema linear: o tempo a partir de i e um mais a media dos
/// tempos dos vizinhos. Em fracao ele tem resposta exata.
///
/// E ai cai um teorema que vale conferir de verdade: o de KAC. O tempo medio de
/// RETORNO a um estado e exatamente 1 dividido pela probabilidade estacionaria
/// dele. Nao aproximadamente: exatamente, e em fracao da para exigir igualdade.
/// Um estado que o passeio visita um sexto do tempo tem retorno medio de seis
/// passos, e isso vale em qualquer cadeia irredutivel, por mais torta que seja.
/// </summary>
public static class Tempos
{
    /// <summary>
    /// O tempo medio ate CHEGAR no alvo, a partir de cada estado, em fracao.
    ///
    /// O tempo do proprio alvo e zero por definicao: ja chegou.
    /// </summary>
    public static Fracao[]? Chegada(Cadeia cadeia, int alvo)
    {
        ArgumentNullException.ThrowIfNull(cadeia);

        var n = cadeia.Estados;
        var a = new Fracao[n, n + 1];

        for (var i = 0; i < n; i++)
        {
            if (i == alvo)
            {
                a[i, i] = Fracao.Um;
                a[i, n] = Fracao.Zero;
                continue;
            }

            for (var j = 0; j < n; j++)
                a[i, j] = (i == j ? Fracao.Um : Fracao.Zero) - cadeia.Transicoes[i, j];

            a[i, alvo] = i == alvo ? Fracao.Um : Fracao.Zero;
            a[i, n] = Fracao.Um;
        }

        return Exata.Resolver(a, n);
    }

    /// <summary>
    /// O tempo medio de RETORNO a um estado: um passo mais a media dos tempos de
    /// chegada dos vizinhos.
    ///
    /// E este o numero que o teorema de Kac promete ser 1/pi.
    /// </summary>
    public static Fracao? Retorno(Cadeia cadeia, int estado)
    {
        ArgumentNullException.ThrowIfNull(cadeia);

        var chegada = Chegada(cadeia, estado);
        if (chegada is null) return null;

        var soma = Fracao.Um;
        for (var j = 0; j < cadeia.Estados; j++) soma += cadeia.Transicoes[estado, j] * chegada[j];

        return soma;
    }

    /// <summary>
    /// O teorema de KAC conferido em igualdade exata: o retorno medio vezes a
    /// probabilidade estacionaria tem que dar exatamente um.
    /// </summary>
    public static bool KacConfere(Cadeia cadeia, int estado)
    {
        ArgumentNullException.ThrowIfNull(cadeia);

        var estacionaria = Exata.Estacionaria(cadeia);
        var retorno = Retorno(cadeia, estado);

        if (estacionaria is null || retorno is null || estacionaria[estado].EhZero) return false;

        return retorno.Value * estacionaria[estado] == Fracao.Um;
    }

    /// <summary>
    /// O maior tempo de chegada da cadeia inteira, que e o numero que diz quao
    /// longe os cantos dela ficam uns dos outros.
    /// </summary>
    public static Fracao? MaiorChegada(Cadeia cadeia)
    {
        ArgumentNullException.ThrowIfNull(cadeia);

        Fracao? maior = null;

        for (var alvo = 0; alvo < cadeia.Estados; alvo++)
        {
            var tempos = Chegada(cadeia, alvo);
            if (tempos is null) return null;

            foreach (var t in tempos)
                if (maior is null || t > maior.Value) maior = t;
        }

        return maior;
    }
}
