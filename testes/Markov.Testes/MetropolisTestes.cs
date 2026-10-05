using System.Numerics;
using Conde.Markov;
using Xunit;

namespace Conde.Markov.Testes;

public class MetropolisTestes
{
    /// <summary>
    /// O ACHADO: a estacionaria da cadeia de Metropolis e EXATAMENTE a
    /// distribuicao alvo, e nenhum lugar do codigo soma os pesos para
    /// normalizar.
    ///
    /// A razao entre dois pesos cancela a soma que ninguem consegue calcular, e
    /// e esse cancelamento que permite amostrar de uma distribuicao com mais
    /// estados do que da para enumerar.
    /// </summary>
    [Fact]
    public void AEstacionariaEhExatamenteOAlvo()
    {
        foreach (var pesos in new[]
                 {
                     new BigInteger[] { 1, 1, 1 },
                     new BigInteger[] { 1, 7, 100, 3 },
                     new BigInteger[] { 2, 3, 5, 7, 11 },
                 })
        {
            var cadeia = Metropolis.Montar(pesos);
            var alvo = Metropolis.Alvo(pesos);
            var estacionaria = Exata.Estacionaria(cadeia)!;

            for (var i = 0; i < pesos.Length; i++) Assert.Equal(alvo[i], estacionaria[i]);
        }
    }

    /// <summary>
    /// E vale com pesos astronomicos, que e o caso que importa: a normalizacao
    /// de verdade seria impossivel de calcular, e a razao continua barata.
    /// </summary>
    [Fact]
    public void EValeComPesosAstronomicos()
    {
        var pesos = new[]
        {
            BigInteger.Pow(10, 40),
            BigInteger.Pow(10, 40) * 3,
            BigInteger.Pow(10, 41),
            BigInteger.Pow(2, 200),
        };

        var estacionaria = Exata.Estacionaria(Metropolis.Montar(pesos))!;
        var alvo = Metropolis.Alvo(pesos);

        for (var i = 0; i < pesos.Length; i++) Assert.Equal(alvo[i], estacionaria[i]);
    }

    /// <summary>
    /// A cadeia construida satisfaz o EQUILIBRIO DETALHADO com o alvo, que e a
    /// razao de ela funcionar.
    /// </summary>
    [Fact]
    public void ACadeiaSatisfazOEquilibrioDetalhado()
    {
        var pesos = new BigInteger[] { 1, 7, 100, 3 };
        Assert.True(Metropolis.EquilibrioDetalhado(Metropolis.Montar(pesos), Metropolis.Alvo(pesos)));
    }

    /// <summary>
    /// E a volta e FALSA: o ciclo de quatro tem estacionaria uniforme e NAO tem
    /// equilibrio detalhado nenhum.
    ///
    /// Ele so anda para frente, entao mantem a uniforme sem nunca estar em
    /// equilibrio entre dois estados. Equilibrio detalhado implica estacionaria;
    /// estacionaria nao implica equilibrio detalhado.
    /// </summary>
    [Fact]
    public void EstacionariaNaoImplicaEquilibrioDetalhado()
    {
        var ciclo = Cadeia.Ciclo();
        var uniforme = Exata.Estacionaria(ciclo)!;

        Assert.True(Exata.EhEstacionaria(ciclo, uniforme));
        Assert.False(Metropolis.EquilibrioDetalhado(ciclo, uniforme));
    }

    /// <summary>
    /// Com pesos iguais a cadeia nunca recusa, e com um peso dominando ela passa
    /// quase todo o tempo parada la. A receita continua certa, e fica cara
    /// exatamente onde a distribuicao e mais torta.
    /// </summary>
    [Fact]
    public void ATaxaDeRecusaCresceComADesigualdade()
    {
        var iguais = Metropolis.TaxaDeRecusa(Metropolis.Montar(1, 1, 1, 1));
        var pouco = Metropolis.TaxaDeRecusa(Metropolis.Montar(1, 2, 4, 8));
        var muito = Metropolis.TaxaDeRecusa(Metropolis.Montar(1, 10, 100, 1000));
        var absurdo = Metropolis.TaxaDeRecusa(Metropolis.Montar(1, 1, 1, 1_000_000));

        Assert.Equal(Fracao.Zero, iguais);
        Assert.True(pouco < muito);
        Assert.True(muito < absurdo);
        Assert.True(absurdo < Fracao.Um);
        Assert.True(absurdo > new Fracao(999, 1000));
    }

    /// <summary>
    /// Recusar nao e desperdicio: ficar parado E o passo. Sem isso as linhas nao
    /// somam um e a cadeia nem existe.
    /// </summary>
    [Fact]
    public void FicarParadoEhOQueFazALinhaSomarUm()
    {
        var cadeia = Metropolis.Montar(1, 10, 100);

        for (var i = 0; i < 3; i++)
        {
            var soma = Fracao.Zero;
            for (var j = 0; j < 3; j++) soma += cadeia.Transicoes[i, j];
            Assert.Equal(Fracao.Um, soma);
        }

        // o estado pesado quase nunca sai
        Assert.True(cadeia.Transicoes[2, 2] > new Fracao(9, 10));

        // e o leve sempre aceita subir
        Assert.Equal(new Fracao(1, 2), cadeia.Transicoes[0, 2]);
    }

    /// <summary>
    /// A cadeia construida converge: ela e irredutivel e aperiodica, entao a
    /// iteracao chega no alvo e nao so passeia perto dele.
    /// </summary>
    [Fact]
    public void ACadeiaConstruidaConverge()
    {
        var pesos = new BigInteger[] { 1, 7, 100, 3 };
        var cadeia = Metropolis.Montar(pesos);

        Assert.True(cadeia.Irredutivel());
        Assert.True(cadeia.Aperiodica());

        var (fim, _, parou) = Potencia.Iterar(cadeia);
        Assert.True(parou);

        var alvo = Metropolis.Alvo(pesos).Select(f => f.EmDouble()).ToArray();
        Assert.True(Exata.Variacao(fim, alvo) < 1e-9);
    }

    [Fact]
    public void UmEstadoSoNaoEhCadeiaDeMetropolis()
    {
        Assert.Throws<ArgumentException>(() => Metropolis.Montar(5));
    }

    [Fact]
    public void OAlvoSomaExatamenteUm()
    {
        var soma = Fracao.Zero;
        foreach (var p in Metropolis.Alvo(3, 5, 7, 11)) soma += p;

        Assert.Equal(Fracao.Um, soma);
    }
}
