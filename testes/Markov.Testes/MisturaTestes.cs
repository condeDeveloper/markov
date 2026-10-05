using Conde.Markov;
using Xunit;

namespace Conde.Markov.Testes;

public class MisturaTestes
{
    /// <summary>
    /// O tempo de mistura e medido no PIOR ponto de partida, e o pior importa: a
    /// media esconde o caso que manda.
    /// </summary>
    [Fact]
    public void OPiorEhOMaiorDeTodosOsComecos()
    {
        foreach (var cadeia in Cadeia.Boas())
        {
            var pior = Mistura.Pior(cadeia);

            for (var inicio = 0; inicio < cadeia.Estados; inicio++)
                Assert.True(Mistura.Tempo(cadeia, inicio) <= pior);

            Assert.Contains(Enumerable.Range(0, cadeia.Estados), i => Mistura.Tempo(cadeia, i) == pior);
        }
    }

    /// <summary>
    /// O ACHADO: cada fator de dez na exigencia custa a MESMA quantidade de
    /// passos a mais, e nao dez vezes mais. O custo cresce com o logaritmo da
    /// exigencia, porque a distancia cai por um fator constante a cada passo.
    /// </summary>
    [Fact]
    public void OCustoCresceComOLogaritmoDaExigencia()
    {
        var cadeia = Cadeia.Pegajosa();

        var passos = new[] { 0.25, 0.025, 0.0025, 0.00025 }
            .Select(limiar => Mistura.PassosAte(cadeia, limiar))
            .ToArray();

        var primeiro = passos[1] - passos[0];
        var segundo = passos[2] - passos[1];
        var terceiro = passos[3] - passos[2];

        Assert.Equal(primeiro, segundo);
        Assert.Equal(segundo, terceiro);
        Assert.True(primeiro > 0);
    }

    [Fact]
    public void ORelaxamentoEhOQueDiz()
    {
        Assert.Equal(2, Mistura.Relaxamento(0.5), 12);
        Assert.Equal(500, Mistura.Relaxamento(0.998), 9);
        Assert.True(double.IsInfinity(Mistura.Relaxamento(1)));
    }

    /// <summary>
    /// A cadeia PREGUICOSA tem a mesma estacionaria, em igualdade exata: ficar
    /// parado com metade de probabilidade nao muda onde a cadeia passa o tempo.
    /// </summary>
    [Fact]
    public void APreguicaNaoMudaAEstacionaria()
    {
        foreach (var cadeia in Cadeia.Todas())
        {
            var antes = Exata.Estacionaria(cadeia);
            var depois = Exata.Estacionaria(Mistura.Preguicosa(cadeia));

            if (antes is null) { Assert.Null(depois); continue; }

            Assert.NotNull(depois);
            for (var i = 0; i < antes.Length; i++) Assert.Equal(antes[i], depois![i]);
        }
    }

    /// <summary>
    /// E ela quebra a periodicidade, que e o que ela existe para fazer: o ciclo
    /// de quatro passa de periodo quatro para periodo um e passa a convergir.
    /// </summary>
    [Fact]
    public void APreguicaQuebraAPeriodicidade()
    {
        var preguicosa = Mistura.Preguicosa(Cadeia.Ciclo());

        Assert.Equal(1, preguicosa.Periodo());
        Assert.True(preguicosa.Irredutivel());
        Assert.True(Potencia.Iterar(preguicosa).Parou);
    }

    /// <summary>
    /// O preco e ficar mais LENTA: os autovalores viram (1 + lambda)/2, entao o
    /// segundo sobe e a mistura demora mais. Nas cadeias que ja convergiam, o
    /// tempo de mistura mais ou menos dobra.
    /// </summary>
    [Fact]
    public void APreguicaCustaQuaseODobroDeMistura()
    {
        foreach (var cadeia in Cadeia.Boas())
        {
            var antes = Mistura.Pior(cadeia);
            var depois = Mistura.Pior(Mistura.Preguicosa(cadeia));

            Assert.True(depois > antes, $"{cadeia.Nome}: antes {antes}, depois {depois}");
            Assert.InRange((double)depois / antes, 1.5, 2.5);
        }
    }

    /// <summary>
    /// E o segundo autovalor da preguicosa e exatamente a media do antigo com
    /// um, medido.
    /// </summary>
    [Fact]
    public void OSegundoAutovalorDaPreguicosaEhAMediaComUm()
    {
        foreach (var cadeia in new[] { Cadeia.Tempo(), Cadeia.Pegajosa() })
        {
            var antes = Potencia.SegundoAutovalorExato(cadeia).EmDouble();
            var depois = Potencia.SegundoAutovalorMedido(Mistura.Preguicosa(cadeia), 4000);

            Assert.Equal((1 + antes) / 2, depois, 5);
        }
    }

    [Fact]
    public void NaCadeiaPeriodicaAMisturaNaoAcontece()
    {
        Assert.Equal(1000, Mistura.Pior(Cadeia.Ciclo(), 0.25, 1000));
    }

    /// <summary>
    /// Exigir menos nunca custa mais: o tempo com limiar frouxo e menor ou igual
    /// ao com limiar apertado.
    /// </summary>
    [Fact]
    public void ExigirMenosNuncaCustaMais()
    {
        foreach (var cadeia in Cadeia.Boas())
            Assert.True(Mistura.Pior(cadeia, 0.4) <= Mistura.Pior(cadeia, 0.01));
    }
}
