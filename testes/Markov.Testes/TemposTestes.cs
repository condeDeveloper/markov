using Conde.Markov;
using Xunit;

namespace Conde.Markov.Testes;

public class TemposTestes
{
    /// <summary>
    /// O teorema de KAC, conferido em IGUALDADE exata: o tempo medio de retorno
    /// a um estado vezes a probabilidade estacionaria dele da exatamente um.
    ///
    /// Nao 0,9999999999999998. A conta inteira e feita em fracao, entao a
    /// igualdade e igualdade, e o teste pode exigir isso de todas as cadeias
    /// irredutiveis do repositorio, inclusive da periodica.
    /// </summary>
    [Fact]
    public void OTeoremaDeKacValeEmIgualdadeExata()
    {
        foreach (var cadeia in Cadeia.Todas())
        {
            if (!cadeia.Irredutivel()) continue;

            for (var estado = 0; estado < cadeia.Estados; estado++)
                Assert.True(Tempos.KacConfere(cadeia, estado),
                            $"{cadeia.Nome}, estado {estado}");
        }
    }

    /// <summary>
    /// E o numero que ele promete e concreto: um estado que o passeio visita um
    /// sexto do tempo tem retorno medio de SEIS passos.
    /// </summary>
    [Fact]
    public void OEstadoDeUmSextoVoltaEmSeisPassos()
    {
        var tempo = Cadeia.Tempo();

        Assert.Equal(new Fracao(1, 6), Exata.Estacionaria(tempo)![1]);
        Assert.Equal(new Fracao(6), Tempos.Retorno(tempo, 1)!.Value);
        Assert.Equal(new Fracao(6, 5), Tempos.Retorno(tempo, 0)!.Value);
    }

    [Fact]
    public void OTempoDeChegadaAoProprioEstadoEhZero()
    {
        foreach (var cadeia in Cadeia.Todas())
        {
            if (!cadeia.Irredutivel()) continue;

            for (var alvo = 0; alvo < cadeia.Estados; alvo++)
                Assert.Equal(Fracao.Zero, Tempos.Chegada(cadeia, alvo)![alvo]);
        }
    }

    /// <summary>
    /// Os tempos de chegada resolvem a propria equacao que os define: o tempo a
    /// partir de i e um mais a media dos tempos dos vizinhos.
    /// </summary>
    [Fact]
    public void OsTemposDeChegadaResolvemAEquacaoQueOsDefine()
    {
        foreach (var cadeia in Cadeia.Todas())
        {
            if (!cadeia.Irredutivel()) continue;

            for (var alvo = 0; alvo < cadeia.Estados; alvo++)
            {
                var h = Tempos.Chegada(cadeia, alvo)!;

                for (var i = 0; i < cadeia.Estados; i++)
                {
                    if (i == alvo) continue;

                    var soma = Fracao.Um;
                    for (var j = 0; j < cadeia.Estados; j++) soma += cadeia.Transicoes[i, j] * h[j];

                    Assert.Equal(h[i], soma);
                }
            }
        }
    }

    /// <summary>
    /// Atravessar uma linha de CINCO estados custa vinte passos em media. O
    /// passeio aleatorio desperdica quase tudo voltando por onde veio, e esse e
    /// o motivo de o tempo de mistura dele crescer com o quadrado do tamanho.
    /// </summary>
    [Fact]
    public void AtravessarALinhaDeCincoCustaVintePassos()
    {
        var chegada = Tempos.Chegada(Cadeia.Linha(), 4)!;

        Assert.Equal(new Fracao(20), chegada[0]);
        Assert.Equal(new Fracao(18), chegada[1]);
        Assert.Equal(new Fracao(14), chegada[2]);
        Assert.Equal(new Fracao(8), chegada[3]);
        Assert.Equal(Fracao.Zero, chegada[4]);
    }

    /// <summary>
    /// E o custo cresce com o QUADRADO do tamanho da linha: dobrar o numero de
    /// estados multiplica a travessia por quatro.
    /// </summary>
    [Fact]
    public void ATravessiaCresceComOQuadradoDoTamanho()
    {
        var curta = Tempos.Chegada(Cadeia.Linha(5), 4)![0].EmDouble();
        var longa = Tempos.Chegada(Cadeia.Linha(10), 9)![0].EmDouble();

        Assert.InRange(longa / curta, 3.5, 4.5);
    }

    /// <summary>
    /// No ciclo, o tempo de chegada cresce LINEAR com a distancia, porque o
    /// passeio so anda para frente e nunca volta: nao ha desperdicio nenhum.
    /// </summary>
    [Fact]
    public void NoCicloAChegadaEhLinearNaDistancia()
    {
        var chegada = Tempos.Chegada(Cadeia.Ciclo(6), 0)!;

        for (var i = 1; i < 6; i++) Assert.Equal(new Fracao(6 - i), chegada[i]);
    }

    [Fact]
    public void OTempoDeRetornoEhSempreMaiorQueUm()
    {
        foreach (var cadeia in Cadeia.Todas())
        {
            if (!cadeia.Irredutivel()) continue;

            for (var estado = 0; estado < cadeia.Estados; estado++)
                Assert.True(Tempos.Retorno(cadeia, estado)!.Value >= Fracao.Um);
        }
    }

    [Fact]
    public void AMaiorChegadaEhAMaiorDeTodas()
    {
        var linha = Cadeia.Linha();
        var maior = Tempos.MaiorChegada(linha)!.Value;

        for (var alvo = 0; alvo < linha.Estados; alvo++)
            foreach (var t in Tempos.Chegada(linha, alvo)!)
                Assert.True(t <= maior);

        Assert.Equal(new Fracao(20), maior);
    }

    [Fact]
    public void NaCadeiaRedutivelKacNaoConfere()
    {
        Assert.False(Tempos.KacConfere(Cadeia.DuasIlhas(), 0));
    }
}
