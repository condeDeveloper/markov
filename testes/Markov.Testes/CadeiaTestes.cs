using Conde.Markov;
using Xunit;

namespace Conde.Markov.Testes;

public class CadeiaTestes
{
    [Fact]
    public void TodaCadeiaTemLinhasQueSomamUm()
    {
        foreach (var cadeia in Cadeia.Todas())
            for (var i = 0; i < cadeia.Estados; i++)
            {
                var soma = Fracao.Zero;
                for (var j = 0; j < cadeia.Estados; j++) soma += cadeia.Transicoes[i, j];

                Assert.Equal(Fracao.Um, soma);
            }
    }

    /// <summary>
    /// A conferencia e EXATA, e e por isso que ela vale: uma linha com tres
    /// tercos passa, e uma linha errada por um milionesimo nao passa. Em ponto
    /// flutuante as duas precisariam da mesma tolerancia.
    /// </summary>
    [Fact]
    public void LinhaQueNaoSomaUmEhRecusada()
    {
        var m = new Fracao[2, 2];
        m[0, 0] = new Fracao(1, 2);
        m[0, 1] = new Fracao(1, 2);
        m[1, 0] = new Fracao(999998, 1000000);
        m[1, 1] = new Fracao(1, 1000000);

        Assert.Throws<ArgumentException>(() => new Cadeia("quase", m));
    }

    [Fact]
    public void TresTercosPassam()
    {
        var m = new Fracao[3, 3];
        for (var i = 0; i < 3; i++)
            for (var j = 0; j < 3; j++)
                m[i, j] = new Fracao(1, 3);

        var cadeia = new Cadeia("tercos", m);
        Assert.Equal(3, cadeia.Estados);
    }

    [Fact]
    public void ProbabilidadeNegativaEhRecusada()
    {
        var m = new Fracao[2, 2];
        m[0, 0] = new Fracao(3, 2);
        m[0, 1] = new Fracao(-1, 2);
        m[1, 0] = Fracao.Um;
        m[1, 1] = Fracao.Zero;

        Assert.Throws<ArgumentException>(() => new Cadeia("negativa", m));
    }

    [Fact]
    public void MatrizNaoQuadradaEhRecusada()
    {
        Assert.Throws<ArgumentException>(() => new Cadeia("torta", new Fracao[2, 3]));
    }

    [Fact]
    public void AIrredutibilidadeEhOQueDiz()
    {
        Assert.True(Cadeia.Tempo().Irredutivel());
        Assert.True(Cadeia.Linha().Irredutivel());
        Assert.True(Cadeia.Ciclo().Irredutivel());
        Assert.False(Cadeia.DuasIlhas().Irredutivel());
    }

    /// <summary>
    /// O ciclo de quatro tem periodo quatro, e a linha com paredes tem periodo
    /// um: o laco nas pontas e o que quebra a alternancia.
    /// </summary>
    [Fact]
    public void OPeriodoEhOQueDiz()
    {
        Assert.Equal(4, Cadeia.Ciclo().Periodo());
        Assert.Equal(3, Cadeia.Ciclo(3).Periodo());
        Assert.Equal(1, Cadeia.Linha().Periodo());
        Assert.Equal(1, Cadeia.Tempo().Periodo());

        Assert.False(Cadeia.Ciclo().Aperiodica());
        Assert.True(Cadeia.Linha().Aperiodica());
    }

    /// <summary>
    /// O periodo nao depende do estado em que a conta comeca, numa cadeia
    /// irredutivel: ele e propriedade da cadeia inteira.
    /// </summary>
    [Fact]
    public void OPeriodoNaoDependeDoEstadoEscolhido()
    {
        foreach (var cadeia in Cadeia.Todas())
        {
            if (!cadeia.Irredutivel()) continue;

            var doZero = cadeia.Periodo();
            for (var estado = 1; estado < cadeia.Estados; estado++)
                Assert.Equal(doZero, cadeia.Periodo(estado));
        }
    }

    [Fact]
    public void AMatrizEmDoubleSomaUmNaCasaDoDouble()
    {
        foreach (var cadeia in Cadeia.Todas())
        {
            var m = cadeia.EmDouble();

            for (var i = 0; i < cadeia.Estados; i++)
            {
                var soma = 0.0;
                for (var j = 0; j < cadeia.Estados; j++) soma += m[i, j];

                Assert.Equal(1, soma, 12);
            }
        }
    }

    [Fact]
    public void AsBoasSaoAsQueConvergem()
    {
        foreach (var cadeia in Cadeia.Boas())
        {
            Assert.True(cadeia.Irredutivel());
            Assert.True(cadeia.Aperiodica());
        }

        Assert.DoesNotContain(Cadeia.Boas(), c => c.Nome.StartsWith("ciclo", StringComparison.Ordinal));
        Assert.DoesNotContain(Cadeia.Boas(), c => c.Nome == "duas ilhas");
    }
}
