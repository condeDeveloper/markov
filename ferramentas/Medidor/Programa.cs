using System.Numerics;
using Conde.Markov;

namespace Conde.Markov.Medidor;

/// <summary>
/// As medidas. O juiz e sempre a conta EXATA em fracao, e o custo e contado em
/// passos da iteracao: nenhuma medida depende de relogio nem de maquina.
/// </summary>
public static class Programa
{
    public static int Main(string[] argumentos)
    {
        var qual = argumentos.Length > 0 ? argumentos[0] : "tudo";
        switch (qual)
        {
            case "estacionaria": Estacionaria(); break;
            case "velocidade": Velocidade(); break;
            case "periodica": Periodica(); break;
            case "kac": Kac(); break;
            case "pagerank": Rank(); break;
            case "metropolis": Metropolis(); break;
            case "tudo": Estacionaria(); Velocidade(); Periodica(); Kac(); Rank(); Metropolis(); break;
            default:
                Console.Error.WriteLine("medidas: estacionaria, velocidade, periodica, kac, pagerank, metropolis, tudo");
                return 1;
        }
        return 0;
    }

    /// <summary>A estacionaria exata, e o que a iteracao entrega.</summary>
    private static void Estacionaria()
    {
        Console.WriteLine("== a distribuicao estacionaria exata, em fracao ==");
        Console.WriteLine();

        foreach (var cadeia in Cadeia.Todas())
        {
            var exata = Exata.Estacionaria(cadeia);
            var texto = exata is null ? "nao e unica" : string.Join("  ", exata.Select(f => f.ToString()));

            Console.WriteLine($"{cadeia.Nome,-16}{(cadeia.Irredutivel() ? "irredutivel" : "redutivel"),-14}" +
                              $"{(cadeia.Aperiodica() ? "aperiodica" : $"periodo {cadeia.Periodo()}"),-14}{texto}");
        }

        Console.WriteLine();
        Console.WriteLine("a cadeia do tempo tem estacionaria 5/6 e 1/6, com denominador inteiro, e nao");
        Console.WriteLine("0,8333333333333334 e 0,16666666666666666. Ter a resposta certa e o que");
        Console.WriteLine("permite dizer que a iteracao chegou nela, em vez de so ter parado de mudar");
        Console.WriteLine();
        Console.WriteLine("a cadeia de DUAS ILHAS nao tem estacionaria no singular: o sistema fica com");
        Console.WriteLine("pivo zero e a eliminacao devolve nulo. Cada mistura das duas ilhas e uma");
        Console.WriteLine("estacionaria diferente, e falar em 'a' estacionaria ali e erro");
        Console.WriteLine();

        Console.WriteLine($"  e o que a iteracao de potencia entrega quando decide parar:");
        Console.WriteLine($"  {"cadeia",-16}{"passos",10}{"distancia ate a exata",24}");

        foreach (var cadeia in Cadeia.Boas())
        {
            var (fim, passos, _) = Potencia.Iterar(cadeia);
            var alvo = Exata.EstacionariaEmDouble(cadeia)!;

            Console.WriteLine($"  {cadeia.Nome,-16}{passos,10:N0}{Exata.Variacao(fim, alvo),24:E2}");
        }

        Console.WriteLine();
        Console.WriteLine("  'parou de mudar' e 'chegou na resposta' nao sao a mesma coisa, e a");
        Console.WriteLine("  distancia da ultima coluna e a diferenca entre as duas");
        Console.WriteLine();
    }

    /// <summary>O segundo autovalor e o tempo de mistura.</summary>
    private static void Velocidade()
    {
        Console.WriteLine("== a velocidade e o segundo autovalor ==");
        Console.WriteLine();
        Console.WriteLine($"{"cadeia",-16}{"lambda medido",16}{"lambda exato",16}{"relaxamento",14}{"mistura",10}");

        foreach (var cadeia in Cadeia.Boas())
        {
            var medido = Potencia.SegundoAutovalorMedido(cadeia, 4000);
            var exato = cadeia.Estados == 2 ? Potencia.SegundoAutovalorExato(cadeia).ToString() : "-";

            Console.WriteLine($"{cadeia.Nome,-16}{medido,16:N6}{exato,16}" +
                              $"{Mistura.Relaxamento(medido),14:N1}{Mistura.Pior(cadeia),10:N0}");
        }

        Console.WriteLine();
        Console.WriteLine("na cadeia de dois estados o segundo autovalor tem formula fechada, 1 - p - q,");
        Console.WriteLine("e a medida bate com ela: a pegajosa da 499/500, que e 0,998, e a do tempo da");
        Console.WriteLine("2/5. A medida nao calcula autovalor nenhum, ela so olha por quanto a");
        Console.WriteLine("distancia encolhe a cada passo");
        Console.WriteLine();
        Console.WriteLine("o tempo de mistura e medido no PIOR ponto de partida, e o pior importa: a");
        Console.WriteLine("media esconde o caso que manda");
        Console.WriteLine();

        Console.WriteLine($"  e o custo de exigir mais, na cadeia pegajosa:");
        Console.WriteLine($"  {"limiar",12}{"passos",10}");

        foreach (var limiar in new[] { 0.25, 0.025, 0.0025, 0.00025 })
            Console.WriteLine($"  {limiar,12:N5}{Mistura.PassosAte(Cadeia.Pegajosa(), limiar),10:N0}");

        Console.WriteLine();
        Console.WriteLine("  cada fator de dez na exigencia custa a MESMA quantidade de passos a mais,");
        Console.WriteLine("  e nao dez vezes mais: o custo cresce com o logaritmo da exigencia, porque");
        Console.WriteLine("  a distancia cai por um fator constante a cada passo");
        Console.WriteLine();
    }

    /// <summary>A cadeia periodica, que tem estacionaria e nao converge.</summary>
    private static void Periodica()
    {
        Console.WriteLine("== a cadeia que tem estacionaria e NAO converge ==");
        Console.WriteLine();

        var ciclo = Cadeia.Ciclo();
        var exata = Exata.Estacionaria(ciclo)!;

        Console.WriteLine($"  o ciclo de quatro tem estacionaria {string.Join("  ", exata.Select(f => f.ToString()))}," +
                          $" e ela e unica: a cadeia e irredutivel");
        Console.WriteLine($"  e ela tem periodo {ciclo.Periodo()}");
        Console.WriteLine();
        Console.WriteLine($"  a distancia ate a estacionaria, passo a passo, saindo do estado zero:");

        var trilha = Potencia.Distancias(ciclo, 9);
        for (var i = 0; i < trilha.Count; i++)
            Console.WriteLine($"    passo {i,2}   {trilha[i],8:N4}");

        Console.WriteLine();
        Console.WriteLine("ela nao encolhe: fica em 0,75 para sempre, porque a distribuicao visita os");
        Console.WriteLine("quatro estados em rodizio e nunca se espalha. A estacionaria existe, e unica");
        Console.WriteLine("e a iteracao de potencia nunca chega nela");
        Console.WriteLine();
        Console.WriteLine("a cura padrao e a cadeia PREGUICOSA: com metade de probabilidade ela fica");
        Console.WriteLine("onde esta. Isso nao muda a estacionaria e quebra a periodicidade");
        Console.WriteLine();

        var preguicosa = Mistura.Preguicosa(ciclo);
        var depois = Exata.Estacionaria(preguicosa)!;

        Console.WriteLine($"  a preguicosa tem periodo {preguicosa.Periodo()} e estacionaria " +
                          $"{string.Join("  ", depois.Select(f => f.ToString()))}");
        Console.WriteLine($"  e ela converge: {Potencia.Iterar(preguicosa).Passos:N0} passos");
        Console.WriteLine();

        Console.WriteLine($"  o preco da preguica, nas cadeias que ja convergiam:");
        Console.WriteLine($"  {"cadeia",-16}{"mistura antes",16}{"mistura depois",17}");

        foreach (var cadeia in Cadeia.Boas())
            Console.WriteLine($"  {cadeia.Nome,-16}{Mistura.Pior(cadeia),16:N0}{Mistura.Pior(Mistura.Preguicosa(cadeia)),17:N0}");

        Console.WriteLine();
        Console.WriteLine("  a preguica deixa a cadeia mais LENTA, e e esse o preco. Os autovalores");
        Console.WriteLine("  viram (1 + lambda)/2, entao o segundo sobe e a mistura demora mais; o que");
        Console.WriteLine("  ela conserta e o menos um, que impedia a convergencia");
        Console.WriteLine();
    }

    /// <summary>O teorema de Kac, conferido em igualdade exata.</summary>
    private static void Kac()
    {
        Console.WriteLine("== o teorema de Kac: tempo de retorno vezes probabilidade = um ==");
        Console.WriteLine();
        Console.WriteLine($"{"cadeia",-16}{"estado",8}{"pi exato",12}{"retorno medio",16}{"produto",10}");

        foreach (var cadeia in Cadeia.Todas())
        {
            if (!cadeia.Irredutivel()) continue;

            var pi = Exata.Estacionaria(cadeia)!;

            for (var estado = 0; estado < cadeia.Estados; estado++)
            {
                var retorno = Tempos.Retorno(cadeia, estado)!.Value;
                Console.WriteLine($"{cadeia.Nome,-16}{estado,8}{pi[estado].ToString(),12}" +
                                  $"{retorno.ToString(),16}{(retorno * pi[estado]).ToString(),10}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("a ultima coluna e UM em todas as linhas, e nao 0,9999999999999998: a conta");
        Console.WriteLine("inteira e feita em fracao, entao a igualdade e igualdade");
        Console.WriteLine();
        Console.WriteLine("o teorema diz uma coisa bonita: um estado que o passeio visita um sexto do");
        Console.WriteLine("tempo tem retorno medio de SEIS passos, e isso vale em qualquer cadeia");
        Console.WriteLine("irredutivel, por mais torta que ela seja");
        Console.WriteLine();

        Console.WriteLine($"  e os tempos de chegada, na linha de cinco estados com paredes:");
        var linha = Cadeia.Linha();
        var chegada = Tempos.Chegada(linha, 4)!;

        for (var i = 0; i < chegada.Length; i++)
            Console.WriteLine($"    do estado {i} ate o 4:   {chegada[i]}");

        Console.WriteLine();
        Console.WriteLine("  atravessar uma linha de cinco estados custa VINTE passos em media, e o");
        Console.WriteLine("  passeio aleatorio desperdica quase tudo voltando por onde veio");
        Console.WriteLine();
        Console.WriteLine("  e os numeros 20, 18, 14 e 8 nao caem linearmente: sair da ponta mais");
        Console.WriteLine("  longe custa so um oitavo a mais que sair do meio, porque o passeio que");
        Console.WriteLine("  comeca no meio ja gasta a maior parte do tempo indo e voltando");
        Console.WriteLine();
    }

    /// <summary>O PageRank e o que o amortecimento compra.</summary>
    private static void Rank()
    {
        Console.WriteLine("== o PageRank e o preco do amortecimento ==");
        Console.WriteLine();
        Console.WriteLine($"{"grafo",-20}{"amortecimento",15}{"razao medida",15}{"passos",10}");

        var grafos = new (string Nome, bool[,] Links)[]
        {
            ("corrente de 8", Pagerank.Corrente(8)),
            ("estrela de 8", Pagerank.Estrela(8)),
            ("duas ilhas de 4", Pagerank.Ilhas(4)),
        };

        foreach (var (nome, links) in grafos)
            foreach (var cima in new[] { 50, 85, 99 })
            {
                var cadeia = Pagerank.Montar(links, cima);
                var razao = Potencia.SegundoAutovalorMedido(cadeia, 400);

                Console.WriteLine($"{nome,-20}{cima / 100.0,15:N2}{razao,15:N6}{Potencia.Iterar(cadeia).Passos,10:N0}");
            }

        Console.WriteLine();
        Console.WriteLine("eu esperava igualdade aqui e a medida corrigiu: a razao fica ABAIXO do");
        Console.WriteLine("amortecimento nas nove linhas, as vezes muito abaixo. Na corrente com");
        Console.WriteLine("amortecimento 0,99 ela mede 0,70, e o limite erra por quase trinta pontos");
        Console.WriteLine();
        Console.WriteLine("o teorema e um LIMITE, nao uma previsao, e so a estrela e as ilhas chegam");
        Console.WriteLine("perto dele. A corrente tem fluxo de mao unica e se espalha mais depressa do");
        Console.WriteLine("que o pior caso previa");
        Console.WriteLine();
        Console.WriteLine("e e exatamente por ser um limite que ele vale a pena: o segundo autovalor");
        Console.WriteLine("da cadeia amortecida nao PASSA do amortecimento, por pior que o grafo seja,");
        Console.WriteLine("entao da para prometer a velocidade sem olhar o grafo");
        Console.WriteLine();
        Console.WriteLine("o amortecimento nao e so o conserto da irredutibilidade: ele e a GARANTIA");
        Console.WriteLine("DE VELOCIDADE, e e ela que permite rodar isso com bilhoes de paginas sem");
        Console.WriteLine("saber nada sobre o grafo");
        Console.WriteLine();
        Console.WriteLine("e ele tem preco: com 0,99 a resposta e mais fiel aos links e a iteracao");
        Console.WriteLine("custa ordens de grandeza mais passos. O 0,85 do artigo original e uma");
        Console.WriteLine("escolha de engenharia entre fidelidade e custo");
        Console.WriteLine();

        Console.WriteLine($"  e o que o amortecimento faz com a resposta, na corrente de cinco:");
        Console.WriteLine($"  {"amortecimento",15}{"pagina 0",12}{"pagina 4",12}{"razao",10}");

        foreach (var cima in new[] { 50, 85, 99 })
        {
            var pi = Exata.Estacionaria(Pagerank.Montar(Pagerank.Corrente(5), cima))!;
            Console.WriteLine($"  {cima / 100.0,15:N2}{pi[0].EmDouble(),12:N4}{pi[4].EmDouble(),12:N4}" +
                              $"{pi[4].EmDouble() / pi[0].EmDouble(),10:N2}");
        }

        Console.WriteLine();
        Console.WriteLine("  com amortecimento alto a ultima pagina da corrente vale muito mais que a");
        Console.WriteLine("  primeira, porque todo o fluxo desce ate ela. Com amortecimento baixo os");
        Console.WriteLine("  pulos aleatorios achatam tudo e o grafo quase deixa de importar");
        Console.WriteLine();
    }

    /// <summary>Metropolis: amostrar sem conhecer a normalizacao.</summary>
    private static void Metropolis()
    {
        Console.WriteLine("== Metropolis: a estacionaria sai exata, sem calcular a normalizacao ==");
        Console.WriteLine();

        var pesos = new BigInteger[] { 1, 7, 100, 3 };
        var cadeia = Conde.Markov.Metropolis.Montar(pesos);
        var alvo = Conde.Markov.Metropolis.Alvo(pesos);
        var estacionaria = Exata.Estacionaria(cadeia)!;

        Console.WriteLine($"  pesos:         {string.Join("  ", pesos.Select(p => p.ToString()))}");
        Console.WriteLine($"  alvo:          {string.Join("  ", alvo.Select(f => f.ToString()))}");
        Console.WriteLine($"  estacionaria:  {string.Join("  ", estacionaria.Select(f => f.ToString()))}");
        Console.WriteLine();

        var bate = true;
        for (var i = 0; i < pesos.Length; i++) bate &= estacionaria[i] == alvo[i];
        Console.WriteLine($"  igualdade exata: {bate}");
        Console.WriteLine();

        var gigantes = new[] { BigInteger.Pow(10, 20), BigInteger.Pow(10, 20) * 3, BigInteger.Pow(10, 21) };
        var grande = Conde.Markov.Metropolis.Montar(gigantes);
        var alvoGrande = Conde.Markov.Metropolis.Alvo(gigantes);
        var estGrande = Exata.Estacionaria(grande)!;

        var bateGrande = true;
        for (var i = 0; i < gigantes.Length; i++) bateGrande &= estGrande[i] == alvoGrande[i];

        Console.WriteLine($"  com pesos na casa de 10^21, a igualdade ainda vale: {bateGrande}");
        Console.WriteLine($"  estacionaria: {string.Join("  ", estGrande.Select(f => f.ToString()))}");
        Console.WriteLine();
        Console.WriteLine("nenhum lugar do codigo soma os pesos para normalizar. A razao entre dois");
        Console.WriteLine("pesos cancela a soma, e e esse cancelamento que permite amostrar de uma");
        Console.WriteLine("distribuicao com mais estados do que da para enumerar");
        Console.WriteLine();

        Console.WriteLine($"  e o preco, que e ficar parado recusando:");
        Console.WriteLine($"  {"pesos",-28}{"taxa de recusa",16}{"chance de sair",17}");

        foreach (var teste in new[]
                 {
                     new BigInteger[] { 1, 1, 1, 1 },
                     new BigInteger[] { 1, 2, 4, 8 },
                     new BigInteger[] { 1, 10, 100, 1000 },
                     new BigInteger[] { 1, 1, 1, 1000000 },
                 })
        {
            var c = Conde.Markov.Metropolis.Montar(teste);
            var recusa = Conde.Markov.Metropolis.TaxaDeRecusa(c);

            Console.WriteLine($"  {string.Join(", ", teste.Select(p => p.ToString())),-28}{recusa.EmDouble(),16:N6}" +
                              $"{(Fracao.Um - recusa).EmDouble(),17:E2}");
        }

        Console.WriteLine();
        Console.WriteLine("  com pesos iguais a cadeia nunca recusa; com um peso um milhao de vezes");
        Console.WriteLine("  maior, a chance de sair do estado pesado e de UM EM DUZENTOS E CINQUENTA");
        Console.WriteLine("  MIL por passo. A receita continua certa, e fica cara exatamente onde a");
        Console.WriteLine("  distribuicao e mais torta, que e onde se costuma precisar dela");
        Console.WriteLine();

        var ciclo = Cadeia.Ciclo();
        var uniforme = Exata.Estacionaria(ciclo)!;

        Console.WriteLine($"  e a volta do equilibrio detalhado e FALSA:");
        Console.WriteLine($"    o ciclo de quatro tem estacionaria uniforme: {Exata.EhEstacionaria(ciclo, uniforme)}");
        Console.WriteLine($"    e tem equilibrio detalhado:                  " +
                          $"{Conde.Markov.Metropolis.EquilibrioDetalhado(ciclo, uniforme)}");
        Console.WriteLine($"    a cadeia de Metropolis tem os dois:          " +
                          $"{Conde.Markov.Metropolis.EquilibrioDetalhado(cadeia, estacionaria)}");
        Console.WriteLine();
        Console.WriteLine("  equilibrio detalhado implica estacionaria, e estacionaria NAO implica");
        Console.WriteLine("  equilibrio detalhado. O ciclo so anda para frente: ele mantem a uniforme");
        Console.WriteLine("  sem nunca estar em equilibrio entre dois estados");
        Console.WriteLine();
    }
}
