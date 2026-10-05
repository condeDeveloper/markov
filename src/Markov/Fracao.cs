using System.Numerics;

namespace Conde.Markov;

/// <summary>
/// Uma FRACAO de inteiros grandes, que e o juiz deste repositorio.
///
/// Distribuicao estacionaria, tempo de retorno e PageRank sao todos solucao de
/// um sistema linear com coeficientes racionais, e em racional eles tem resposta
/// EXATA. Em ponto flutuante nao: a soma das probabilidades da 0,9999999999999998
/// e ninguem sabe se o metodo errou ou se o arredondamento errou.
///
/// Com a fracao, a pergunta "o metodo iterativo converge para a resposta certa?"
/// deixa de ser uma comparacao com tolerancia e vira uma comparacao com a
/// resposta. E o teorema de Kac, que diz que o tempo medio de retorno a um
/// estado e exatamente o inverso da probabilidade dele, vira igualdade e nao
/// aproximacao.
/// </summary>
public readonly struct Fracao : IEquatable<Fracao>
{
    public Fracao(BigInteger cima, BigInteger baixo)
    {
        if (baixo.IsZero) throw new DivideByZeroException("fracao com denominador zero");

        if (baixo.Sign < 0) { cima = -cima; baixo = -baixo; }

        var divisor = BigInteger.GreatestCommonDivisor(BigInteger.Abs(cima), baixo);
        if (!divisor.IsOne && !divisor.IsZero) { cima /= divisor; baixo /= divisor; }

        Cima = cima;
        baixo_ = cima.IsZero ? BigInteger.One : baixo;
    }

    public Fracao(BigInteger inteiro) : this(inteiro, BigInteger.One) { }

    private readonly BigInteger baixo_;

    public BigInteger Cima { get; }

    /// <summary>
    /// O denominador, com o zero do default virando um.
    ///
    /// Isso nao e detalhe: uma matriz criada com "new Fracao[n, n]" vem cheia
    /// de default(Fracao), que tem o campo do denominador em ZERO, e somar duas
    /// dessas estoura com divisao por zero num lugar que nao tem divisao
    /// nenhuma escrita. Com o getter, default(Fracao) e exatamente a fracao
    /// zero, e matriz esparsa pode ser montada preenchendo so o que nao e zero.
    /// </summary>
    public BigInteger Baixo => baixo_.IsZero ? BigInteger.One : baixo_;

    public static Fracao Zero => new(BigInteger.Zero);

    public static Fracao Um => new(BigInteger.One);

    public static implicit operator Fracao(int inteiro) => new(inteiro);

    public static Fracao operator +(Fracao a, Fracao b) => new(a.Cima * b.Baixo + b.Cima * a.Baixo, a.Baixo * b.Baixo);

    public static Fracao operator -(Fracao a, Fracao b) => new(a.Cima * b.Baixo - b.Cima * a.Baixo, a.Baixo * b.Baixo);

    public static Fracao operator -(Fracao a) => new(-a.Cima, a.Baixo);

    public static Fracao operator *(Fracao a, Fracao b) => new(a.Cima * b.Cima, a.Baixo * b.Baixo);

    public static Fracao operator /(Fracao a, Fracao b)
    {
        if (b.Cima.IsZero) throw new DivideByZeroException("divisao por fracao zero");
        return new Fracao(a.Cima * b.Baixo, a.Baixo * b.Cima);
    }

    public static bool operator ==(Fracao a, Fracao b) => a.Equals(b);

    public static bool operator !=(Fracao a, Fracao b) => !a.Equals(b);

    public static bool operator <(Fracao a, Fracao b) => (a - b).Cima.Sign < 0;

    public static bool operator >(Fracao a, Fracao b) => (a - b).Cima.Sign > 0;

    public static bool operator <=(Fracao a, Fracao b) => (a - b).Cima.Sign <= 0;

    public static bool operator >=(Fracao a, Fracao b) => (a - b).Cima.Sign >= 0;

    public bool EhZero => Cima.IsZero;

    public int Sinal => Cima.Sign;

    public Fracao Modulo => Cima.Sign < 0 ? -this : this;

    /// <summary>
    /// O valor em ponto flutuante, para comparar com o metodo iterativo.
    ///
    /// A divisao e feita no fim, uma vez so, em vez de a cada passo da conta. E
    /// essa a diferenca entre o juiz e o julgado.
    /// </summary>
    public double EmDouble()
    {
        // Numerador e denominador podem crescer alem do que um double aguenta,
        // e dividir dois infinitos da NaN. Encolher os dois juntos preserva a
        // razao e nao perde nada que o double fosse guardar.
        var cima = Cima;
        var baixo = Baixo;

        while (BigInteger.Abs(cima) > BigInteger.Pow(10, 300) || baixo > BigInteger.Pow(10, 300))
        {
            cima /= 1_000_000;
            baixo /= 1_000_000;
            if (baixo.IsZero) return double.NaN;
        }

        return (double)cima / (double)baixo;
    }

    public bool Equals(Fracao outra) => Cima == outra.Cima && Baixo == outra.Baixo;

    public override bool Equals(object? objeto) => objeto is Fracao outra && Equals(outra);

    public override int GetHashCode() => HashCode.Combine(Cima, Baixo);

    public override string ToString() => Baixo.IsOne ? Cima.ToString() : $"{Cima}/{Baixo}";
}
