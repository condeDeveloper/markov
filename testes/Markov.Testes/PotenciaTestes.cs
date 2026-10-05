using Conde.Markov;
using Xunit;

namespace Conde.Markov.Testes;

public class PotenciaTestes
{
    /// <summary>
    /// Nas cadeias que convergem, a iteracao chega na estacionaria EXATA, e nao
    /// so para de mudar.
    /// </summary>
    [Fact]
    public void AIteracaoChegaNaEstacionariaExata()
    {
        foreach (var cadeia in Cadeia.Boas())
        {
            var (fim, _, parou) = Potencia.Iterar(cadeia);
            var alvo = Exata.EstacionariaEmDouble(cadeia)!;

            Assert.True(parou, $"{cadeia.Nome} nao parou");
            Assert.True(Exata.Variacao(fim, alvo) < 1e-8, $"{cadeia.Nome} parou a {Exata.Variacao(fim, alvo)}");
        }
    }

    /// <summary>
    /// E chega no mesmo lugar saindo de qualquer estado: a estacionaria nao
    /// depende de onde o passeio comecou, que e a propriedade que da nome a ela.
    /// </summary>
    [Fact]
    public void ELaChegaDeQualquerPontoDePartida()
    {
        foreach (var cadeia in Cadeia.Boas())
        {
            var alvo = Exata.EstacionariaEmDouble(cadeia)!;

            for (var inicio = 0; inicio < cadeia.Estados; inicio++)
            {
                var (fim, _, _) = Potencia.Iterar(cadeia, Potencia.Concentrada(cadeia.Estados, inicio));
                Assert.True(Exata.Variacao(fim, alvo) < 1e-8);
            }
        }
    }

    /// <summary>
    /// O ACHADO: o ciclo de quatro tem estacionaria unica e a iteracao NUNCA
    /// chega nela. A distancia fica em 0,75 para sempre, porque a distribuicao
    /// visita os quatro estados em rodizio e nunca se espalha.
    /// </summary>
    [Fact]
    public void NoCicloAIteracaoNuncaConverge()
    {
        var ciclo = Cadeia.Ciclo();

        Assert.NotNull(Exata.Estacionaria(ciclo));

        var trilha = Potencia.Distancias(ciclo, 500);
        Assert.All(trilha, d => Assert.Equal(0.75, d, 10));

        var (_, passos, parou) = Potencia.Iterar(ciclo, limite: 5000);
        Assert.False(parou);
        Assert.Equal(5000, passos);
    }

    /// <summary>
    /// A distancia ate a estacionaria so encolhe: a cadeia nunca se afasta do
    /// equilibrio, em nenhum passo.
    /// </summary>
    [Fact]
    public void ADistanciaNuncaCresce()
    {
        foreach (var cadeia in Cadeia.Boas())
        {
            var trilha = Potencia.Distancias(cadeia, 100);

            for (var i = 1; i < trilha.Count; i++)
                Assert.True(trilha[i] <= trilha[i - 1] + 1e-15,
                            $"{cadeia.Nome} subiu no passo {i}: de {trilha[i - 1]} para {trilha[i]}");
        }
    }

    /// <summary>
    /// O segundo autovalor medido bate com a formula fechada 1 - p - q nas
    /// cadeias de dois estados. A medida nao calcula autovalor nenhum: ela so
    /// olha por quanto a distancia encolhe a cada passo.
    /// </summary>
    [Fact]
    public void OSegundoAutovalorMedidoBateComAFormulaFechada()
    {
        foreach (var cadeia in new[] { Cadeia.Tempo(), Cadeia.Pegajosa() })
        {
            var exato = Potencia.SegundoAutovalorExato(cadeia).EmDouble();
            var medido = Potencia.SegundoAutovalorMedido(cadeia, 2000);

            Assert.Equal(exato, medido, 5);
        }
    }

    [Fact]
    public void AFormulaFechadaSoValeParaDoisEstados()
    {
        Assert.Equal(new Fracao(2, 5), Potencia.SegundoAutovalorExato(Cadeia.Tempo()));
        Assert.Equal(new Fracao(499, 500), Potencia.SegundoAutovalorExato(Cadeia.Pegajosa()));
        Assert.Throws<ArgumentException>(() => Potencia.SegundoAutovalorExato(Cadeia.Linha()));
    }

    /// <summary>
    /// A cadeia pegajosa custa centenas de vezes mais passos que a do tempo, e a
    /// razao entre os custos segue a razao entre os tempos de relaxamento.
    /// </summary>
    [Fact]
    public void OSegundoAutovalorDitaOCusto()
    {
        var rapida = Potencia.Iterar(Cadeia.Tempo()).Passos;
        var lenta = Potencia.Iterar(Cadeia.Pegajosa()).Passos;

        Assert.True(lenta > rapida * 100, $"rapida {rapida}, lenta {lenta}");
    }

    [Fact]
    public void UmPassoEmDoubleConcordaComUmPassoEmFracao()
    {
        foreach (var cadeia in Cadeia.Todas())
        {
            var emFracao = new Fracao[cadeia.Estados];
            emFracao[0] = Fracao.Um;
            emFracao = Exata.UmPasso(cadeia, emFracao);

            var emDouble = Potencia.UmPasso(cadeia.EmDouble(), Potencia.Concentrada(cadeia.Estados));

            for (var j = 0; j < cadeia.Estados; j++)
                Assert.Equal(emFracao[j].EmDouble(), emDouble[j], 12);
        }
    }

    [Fact]
    public void AConcentradaEhConcentrada()
    {
        var d = Potencia.Concentrada(4, 2);

        Assert.Equal(new[] { 0.0, 0, 1, 0 }, d);
        Assert.Equal(1, d.Sum(), 12);
    }
}
