using System.Numerics;
using Conde.Markov;
using Xunit;

namespace Conde.Markov.Testes;

public class FracaoTestes
{
    [Fact]
    public void AsContasSaoOQueDizem()
    {
        var meio = new Fracao(1, 2);
        var terco = new Fracao(1, 3);

        Assert.Equal(new Fracao(5, 6), meio + terco);
        Assert.Equal(new Fracao(1, 6), meio - terco);
        Assert.Equal(new Fracao(1, 6), meio * terco);
        Assert.Equal(new Fracao(3, 2), meio / terco);
    }

    /// <summary>
    /// Um terco mais um terco mais um terco e exatamente UM, e aqui eu errei
    /// antes de medir: eu esperava que a soma em ponto flutuante desse
    /// 1,0000000000000002, e ela da exatamente um, porque o arredondamento de
    /// 1/3 para baixo e compensado na soma.
    ///
    /// O caso em que o double falha de verdade e outro, e o teste usa ele:
    /// 0,1 mais 0,2 nao e 0,3. A conferencia das linhas da cadeia precisa ser
    /// exata por causa desse, nao daquele.
    /// </summary>
    [Fact]
    public void ASomaDeFracoesEhExataOndeODoubleFalha()
    {
        var terco = new Fracao(1, 3);
        Assert.Equal(Fracao.Um, terco + terco + terco);

        var decimo = new Fracao(1, 10);
        var quinto = new Fracao(1, 5);
        Assert.Equal(new Fracao(3, 10), decimo + quinto);

        Assert.NotEqual(0.3, 0.1 + 0.2);
        Assert.Equal(0.3, (decimo + quinto).EmDouble(), 15);
    }

    [Fact]
    public void AFracaoEhSempreReduzida()
    {
        var f = new Fracao(1000, 2000);

        Assert.Equal(BigInteger.One, f.Cima);
        Assert.Equal(new BigInteger(2), f.Baixo);
        Assert.Equal("1/2", f.ToString());
    }

    [Fact]
    public void OSinalFicaSempreEmCima()
    {
        var f = new Fracao(1, -2);

        Assert.Equal(new BigInteger(-1), f.Cima);
        Assert.Equal(new BigInteger(2), f.Baixo);
        Assert.Equal(new Fracao(-1, 2), f);
    }

    /// <summary>
    /// O default da struct tem o campo do denominador em ZERO, e sem o getter
    /// que troca por um, somar duas delas estoura com divisao por zero num
    /// lugar que nao tem divisao nenhuma escrita.
    ///
    /// Isso importa de verdade: toda matriz esparsa deste repositorio e montada
    /// com "new Fracao[n, n]" e so os valores que nao sao zero sao preenchidos.
    /// </summary>
    [Fact]
    public void ODefaultDaStructEhExatamenteZero()
    {
        var vazia = default(Fracao);

        Assert.True(vazia.EhZero);
        Assert.Equal(Fracao.Zero, vazia);
        Assert.Equal(BigInteger.One, vazia.Baixo);
        Assert.Equal(Fracao.Um, vazia + Fracao.Um);
        Assert.Equal(Fracao.Zero, vazia + vazia);
    }

    [Fact]
    public void AComparacaoOrdenaDireito()
    {
        Assert.True(new Fracao(1, 3) < new Fracao(1, 2));
        Assert.True(new Fracao(-1, 3) < Fracao.Zero);
        Assert.True(new Fracao(7, 2) > new Fracao(3, 1));
        Assert.True(new Fracao(2, 4) <= new Fracao(1, 2));
        Assert.True(new Fracao(2, 4) >= new Fracao(1, 2));
    }

    [Fact]
    public void DenominadorZeroNaoPassa()
    {
        Assert.Throws<DivideByZeroException>(() => new Fracao(1, 0));
        Assert.Throws<DivideByZeroException>(() => Fracao.Um / Fracao.Zero);
    }

    /// <summary>
    /// Numerador e denominador podem crescer alem do que um double aguenta, e
    /// dividir dois infinitos da NaN. Encolher os dois juntos preserva a razao.
    /// </summary>
    [Fact]
    public void FracaoGiganteAindaViraDouble()
    {
        var gigante = new Fracao(BigInteger.Pow(10, 500), BigInteger.Pow(10, 500) * 2);
        Assert.Equal(0.5, gigante.EmDouble(), 10);

        var outra = new Fracao(BigInteger.Pow(7, 600), BigInteger.Pow(7, 600) * 3);
        Assert.Equal(1.0 / 3, outra.EmDouble(), 10);
    }

    [Fact]
    public void OModuloEOSinalSaoOQueDizem()
    {
        Assert.Equal(new Fracao(1, 2), new Fracao(-1, 2).Modulo);
        Assert.Equal(-1, new Fracao(-1, 2).Sinal);
        Assert.Equal(0, Fracao.Zero.Sinal);
        Assert.Equal(1, Fracao.Um.Sinal);
    }

    [Fact]
    public void InteiroViraFracaoSozinho()
    {
        Fracao sete = 7;

        Assert.Equal("7", sete.ToString());
        Assert.Equal(new Fracao(14, 2), sete);
    }
}
