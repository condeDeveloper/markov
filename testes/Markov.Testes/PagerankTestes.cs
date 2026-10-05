using Conde.Markov;
using Xunit;

namespace Conde.Markov.Testes;

public class PagerankTestes
{
    /// <summary>
    /// A cadeia montada e uma cadeia de verdade mesmo com paginas SEM LINK DE
    /// SAIDA. Sem o remendo de sortear uma pagina qualquer a partir delas, a
    /// linha somaria zero e a matriz nem seria estocastica.
    /// </summary>
    [Fact]
    public void PaginaSemSaidaNaoQuebraACadeia()
    {
        var corrente = Pagerank.Corrente(5);
        var cadeia = Pagerank.Montar(corrente);

        // a ultima pagina nao aponta para ninguem
        for (var j = 0; j < 5; j++) Assert.False(corrente[4, j]);

        // e mesmo assim a linha dela e uma distribuicao
        var soma = Fracao.Zero;
        for (var j = 0; j < 5; j++) soma += cadeia.Transicoes[4, j];
        Assert.Equal(Fracao.Um, soma);
    }

    /// <summary>
    /// O amortecimento conserta a irredutibilidade: a corrente crua e redutivel,
    /// e a cadeia do PageRank sobre ela e irredutivel e aperiodica.
    /// </summary>
    [Fact]
    public void OAmortecimentoTornaQualquerGrafoIrredutivel()
    {
        foreach (var links in new[] { Pagerank.Corrente(8), Pagerank.Estrela(8), Pagerank.Ilhas(4) })
        {
            var cadeia = Pagerank.Montar(links);

            Assert.True(cadeia.Irredutivel());
            Assert.True(cadeia.Aperiodica());
            Assert.NotNull(Exata.Estacionaria(cadeia));
        }
    }

    /// <summary>
    /// O ACHADO: eu esperava que a razao de convergencia fosse IGUAL ao
    /// amortecimento, e ela fica ABAIXO dele, as vezes bem abaixo.
    ///
    /// O teorema e um LIMITE, nao uma previsao. E e exatamente por ser um limite
    /// que ele vale: da para prometer a velocidade sem olhar o grafo.
    /// </summary>
    [Fact]
    public void ARazaoFicaAbaixoDoAmortecimentoENaoIgualAEle()
    {
        foreach (var links in new[] { Pagerank.Corrente(8), Pagerank.Estrela(8), Pagerank.Ilhas(4) })
            foreach (var cima in new[] { 50, 85, 99 })
            {
                var razao = Pagerank.RazaoMedida(links, cima, 100, 400);

                Assert.True(razao <= cima / 100.0 + 1e-9, $"a razao {razao} passou do amortecimento {cima / 100.0}");
                Assert.True(razao > 0);
            }

        // e na corrente ela fica bem abaixo: 0,63 contra 0,85
        Assert.True(Pagerank.RazaoMedida(Pagerank.Corrente(8)) < 0.75);
    }

    /// <summary>
    /// Amortecimento maior custa mais passos, em todos os grafos. E a troca de
    /// engenharia do artigo original: fidelidade aos links contra custo.
    /// </summary>
    [Fact]
    public void AmortecimentoMaiorCustaMaisPassos()
    {
        foreach (var links in new[] { Pagerank.Corrente(8), Pagerank.Estrela(8), Pagerank.Ilhas(4) })
        {
            var barato = Potencia.Iterar(Pagerank.Montar(links, 50)).Passos;
            var caro = Potencia.Iterar(Pagerank.Montar(links, 99)).Passos;

            Assert.True(caro > barato * 2, $"barato {barato}, caro {caro}");
        }
    }

    /// <summary>
    /// Na CORRENTE, a ultima pagina vale mais que a primeira, porque todo o
    /// fluxo desce ate ela. E a vantagem cresce com o amortecimento: com pulos
    /// aleatorios demais, o grafo quase deixa de importar.
    /// </summary>
    [Fact]
    public void NaCorrenteAVantagemCresceComOAmortecimento()
    {
        var anterior = 0.0;

        foreach (var cima in new[] { 50, 85, 99 })
        {
            var pi = Exata.Estacionaria(Pagerank.Montar(Pagerank.Corrente(5), cima))!;
            var razao = pi[4].EmDouble() / pi[0].EmDouble();

            Assert.True(razao > anterior, $"com {cima} a razao caiu para {razao}");
            anterior = razao;
        }

        Assert.True(anterior > 4);
    }

    /// <summary>
    /// Na ESTRELA, a pagina que todas apontam ganha, que e a resposta obvia que
    /// qualquer medida de importancia tem que dar.
    /// </summary>
    [Fact]
    public void NaEstrelaOCentroGanha()
    {
        var pi = Exata.Estacionaria(Pagerank.Montar(Pagerank.Estrela(8)))!;

        for (var i = 1; i < 8; i++) Assert.True(pi[0] > pi[i]);
    }

    /// <summary>
    /// E com amortecimento ZERO o grafo some inteiro: a resposta vira uniforme,
    /// qualquer que seja a estrutura de links.
    /// </summary>
    [Fact]
    public void ComAmortecimentoZeroOGrafoSome()
    {
        foreach (var links in new[] { Pagerank.Corrente(5), Pagerank.Estrela(5) })
        {
            var pi = Exata.Estacionaria(Pagerank.Montar(links, 0))!;
            foreach (var p in pi) Assert.Equal(new Fracao(1, 5), p);
        }
    }

    /// <summary>
    /// A estacionaria do PageRank e exata: ela sai em fracao com denominador
    /// inteiro, e e contra ela que a iteracao e julgada.
    /// </summary>
    [Fact]
    public void AEstacionariaDoPagerankSaiExata()
    {
        var cadeia = Pagerank.Montar(Pagerank.Estrela(4));
        var pi = Exata.Estacionaria(cadeia)!;

        Assert.True(Exata.EhEstacionaria(cadeia, pi));

        var soma = Fracao.Zero;
        foreach (var p in pi) soma += p;
        Assert.Equal(Fracao.Um, soma);

        var (fim, _, _) = Potencia.Iterar(cadeia);
        Assert.True(Exata.Variacao(fim, pi.Select(f => f.EmDouble()).ToArray()) < 1e-9);
    }

    [Fact]
    public void AsIlhasSaoSimetricasMenosPeloLinkQueAsLiga()
    {
        var links = Pagerank.Ilhas(3);

        Assert.True(links[0, 1]);
        Assert.False(links[0, 0]);
        Assert.True(links[2, 3]);
        Assert.False(links[3, 2]);
    }
}
