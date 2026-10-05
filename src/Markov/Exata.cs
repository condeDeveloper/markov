namespace Conde.Markov;

/// <summary>
/// A DISTRIBUICAO ESTACIONARIA EXATA, resolvida em fracao.
///
/// Ela e a solucao de pi = pi*P com as probabilidades somando um, que e um
/// sistema linear. Resolver em fracao da a resposta certa, com denominador
/// inteiro e tudo, e e contra ela que a iteracao de potencia e julgada.
///
/// A diferenca entre "o metodo iterativo parou de mudar" e "o metodo iterativo
/// chegou na resposta" so existe quando ha uma resposta. E ela aqui tem nome e
/// denominador: a estacionaria da cadeia do tempo e 5/6 e 1/6, nao
/// 0,8333333333333334 e 0,16666666666666666.
/// </summary>
public static class Exata
{
    /// <summary>
    /// Resolve pi = pi*P exatamente.
    ///
    /// O sistema montado e (P^T - I)*pi = 0 com a ultima equacao trocada pela
    /// soma igual a um. Trocar uma equacao em vez de acrescentar e o que mantem
    /// o sistema quadrado, e a equacao trocada e redundante de qualquer jeito:
    /// as colunas de P^T - I somam zero.
    /// </summary>
    public static Fracao[]? Estacionaria(Cadeia cadeia)
    {
        ArgumentNullException.ThrowIfNull(cadeia);

        var n = cadeia.Estados;
        var a = new Fracao[n, n + 1];

        for (var i = 0; i < n - 1; i++)
        {
            for (var j = 0; j < n; j++)
                a[i, j] = cadeia.Transicoes[j, i] - (i == j ? Fracao.Um : Fracao.Zero);

            a[i, n] = Fracao.Zero;
        }

        for (var j = 0; j < n; j++) a[n - 1, j] = Fracao.Um;
        a[n - 1, n] = Fracao.Um;

        return Resolver(a, n);
    }

    /// <summary>
    /// Eliminacao de Gauss em fracao, com troca de linha quando o pivo e zero.
    ///
    /// Nao ha pivotamento por tamanho aqui, e nao e esquecimento: em aritmetica
    /// exata nao existe pivo pequeno demais, so pivo ZERO. O pivotamento parcial
    /// existe para conter arredondamento, e aqui nao ha arredondamento nenhum.
    /// </summary>
    public static Fracao[]? Resolver(Fracao[,] sistema, int n)
    {
        ArgumentNullException.ThrowIfNull(sistema);

        var a = (Fracao[,])sistema.Clone();

        for (var coluna = 0; coluna < n; coluna++)
        {
            var pivo = -1;
            for (var linha = coluna; linha < n; linha++)
                if (!a[linha, coluna].EhZero) { pivo = linha; break; }

            if (pivo < 0) return null;

            if (pivo != coluna)
                for (var j = coluna; j <= n; j++) (a[coluna, j], a[pivo, j]) = (a[pivo, j], a[coluna, j]);

            for (var linha = 0; linha < n; linha++)
            {
                if (linha == coluna || a[linha, coluna].EhZero) continue;

                var fator = a[linha, coluna] / a[coluna, coluna];
                for (var j = coluna; j <= n; j++) a[linha, j] = a[linha, j] - fator * a[coluna, j];
            }
        }

        var x = new Fracao[n];
        for (var i = 0; i < n; i++) x[i] = a[i, n] / a[i, i];

        return x;
    }

    /// <summary>
    /// Confere que uma distribuicao e mesmo estacionaria: pi*P tem que dar pi, em
    /// igualdade exata.
    /// </summary>
    public static bool EhEstacionaria(Cadeia cadeia, Fracao[] distribuicao)
    {
        ArgumentNullException.ThrowIfNull(cadeia);
        ArgumentNullException.ThrowIfNull(distribuicao);

        var depois = UmPasso(cadeia, distribuicao);

        for (var j = 0; j < cadeia.Estados; j++)
            if (depois[j] != distribuicao[j]) return false;

        return true;
    }

    /// <summary>Um passo da cadeia a partir de uma distribuicao, em fracao.</summary>
    public static Fracao[] UmPasso(Cadeia cadeia, Fracao[] distribuicao)
    {
        ArgumentNullException.ThrowIfNull(cadeia);
        ArgumentNullException.ThrowIfNull(distribuicao);

        var depois = new Fracao[cadeia.Estados];

        for (var j = 0; j < cadeia.Estados; j++)
        {
            var soma = Fracao.Zero;
            for (var i = 0; i < cadeia.Estados; i++) soma += distribuicao[i] * cadeia.Transicoes[i, j];
            depois[j] = soma;
        }

        return depois;
    }

    /// <summary>A estacionaria exata convertida para ponto flutuante, uma vez so no fim.</summary>
    public static double[]? EstacionariaEmDouble(Cadeia cadeia)
    {
        var exata = Estacionaria(cadeia);
        return exata?.Select(f => f.EmDouble()).ToArray();
    }

    /// <summary>
    /// A DISTANCIA DE VARIACAO TOTAL entre duas distribuicoes: metade da soma dos
    /// modulos das diferencas.
    ///
    /// Ela e a medida certa de "quao longe do estacionario", porque vale entre
    /// zero e um e tem significado: e a maior diferenca de probabilidade que
    /// qualquer evento pode ter entre as duas distribuicoes.
    /// </summary>
    public static double Variacao(double[] a, double[] b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        var soma = 0.0;
        for (var i = 0; i < a.Length; i++) soma += Math.Abs(a[i] - b[i]);

        return soma / 2;
    }
}
