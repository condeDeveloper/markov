using Conde.Markov;
using Xunit;

namespace Conde.Markov.Testes;

public class ExataTestes
{
    /// <summary>
    /// A estacionaria da cadeia do tempo e 5/6 e 1/6, com denominador inteiro.
    /// Nao 0,8333333333333334: ter a resposta certa e o que permite dizer que a
    /// iteracao chegou nela, em vez de so ter parado de mudar.
    /// </summary>
    [Fact]
    public void AEstacionariaDoTempoEhCincoSextosEUmSexto()
    {
        var pi = Exata.Estacionaria(Cadeia.Tempo())!;

        Assert.Equal(new Fracao(5, 6), pi[0]);
        Assert.Equal(new Fracao(1, 6), pi[1]);
    }

    [Fact]
    public void AEstacionariaDaLinhaEDoCicloSaoUniformes()
    {
        foreach (var cadeia in new[] { Cadeia.Linha(), Cadeia.Ciclo(), Cadeia.Pegajosa() })
        {
            var pi = Exata.Estacionaria(cadeia)!;
            var esperado = new Fracao(1, cadeia.Estados);

            foreach (var p in pi) Assert.Equal(esperado, p);
        }
    }

    /// <summary>
    /// A estacionaria achada e mesmo estacionaria: multiplicar por P devolve ela
    /// mesma, em IGUALDADE exata e nao com tolerancia.
    /// </summary>
    [Fact]
    public void AEstacionariaAchadaEhMesmoEstacionaria()
    {
        foreach (var cadeia in Cadeia.Todas())
        {
            var pi = Exata.Estacionaria(cadeia);
            if (pi is null) continue;

            Assert.True(Exata.EhEstacionaria(cadeia, pi), $"{cadeia.Nome} falhou");
        }
    }

    [Fact]
    public void AEstacionariaSomaExatamenteUm()
    {
        foreach (var cadeia in Cadeia.Todas())
        {
            var pi = Exata.Estacionaria(cadeia);
            if (pi is null) continue;

            var soma = Fracao.Zero;
            foreach (var p in pi) soma += p;

            Assert.Equal(Fracao.Um, soma);
            Assert.All(pi, p => Assert.True(p.Sinal >= 0));
        }
    }

    /// <summary>
    /// A cadeia REDUTIVEL nao tem estacionaria no singular, e o sistema acusa
    /// isso com pivo zero. Falar em "a" estacionaria ali e erro, e devolver uma
    /// delas em silencio seria pior que devolver nulo.
    /// </summary>
    [Fact]
    public void ACadeiaRedutivelNaoTemEstacionariaUnica()
    {
        Assert.Null(Exata.Estacionaria(Cadeia.DuasIlhas()));
    }

    /// <summary>
    /// E duas estacionarias diferentes dela sao as duas estacionarias: cada
    /// mistura das duas ilhas e uma resposta valida.
    /// </summary>
    [Fact]
    public void EDuasIlhasTemMaisDeUmaEstacionaria()
    {
        var ilhas = Cadeia.DuasIlhas();

        var soNaPrimeira = new[] { new Fracao(1, 2), new Fracao(1, 2), Fracao.Zero, Fracao.Zero };
        var soNaSegunda = new[] { Fracao.Zero, Fracao.Zero, new Fracao(1, 3), new Fracao(2, 3) };

        Assert.True(Exata.EhEstacionaria(ilhas, soNaPrimeira));
        Assert.True(Exata.EhEstacionaria(ilhas, soNaSegunda));

        var mistura = new Fracao[4];
        for (var i = 0; i < 4; i++) mistura[i] = (soNaPrimeira[i] + soNaSegunda[i]) / new Fracao(2);

        Assert.True(Exata.EhEstacionaria(ilhas, mistura));
    }

    [Fact]
    public void UmPassoNaoMudaASomaDasProbabilidades()
    {
        foreach (var cadeia in Cadeia.Todas())
        {
            var d = new Fracao[cadeia.Estados];
            d[0] = Fracao.Um;

            for (var i = 0; i < 5; i++)
            {
                d = Exata.UmPasso(cadeia, d);

                var soma = Fracao.Zero;
                foreach (var p in d) soma += p;
                Assert.Equal(Fracao.Um, soma);
            }
        }
    }

    [Fact]
    public void ADistanciaDeVariacaoTotalEhOQueDiz()
    {
        Assert.Equal(0, Exata.Variacao([0.5, 0.5], [0.5, 0.5]), 12);
        Assert.Equal(1, Exata.Variacao([1, 0], [0, 1]), 12);
        Assert.Equal(0.25, Exata.Variacao([0.5, 0.5], [0.25, 0.75]), 12);
    }

    [Fact]
    public void SistemaSemSolucaoDevolveNulo()
    {
        var sistema = new Fracao[2, 3];
        Assert.Null(Exata.Resolver(sistema, 2));
    }
}
