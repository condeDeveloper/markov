using System.Numerics;

namespace Conde.Markov;

/// <summary>
/// METROPOLIS e HASTINGS, 1953 e 1970: construir uma cadeia cuja estacionaria e
/// a distribuicao que se quer.
///
/// O problema que ele resolve e concreto. Em geral se conhece o PESO de cada
/// estado, nao a probabilidade: o peso e facil de calcular e a probabilidade
/// exigiria somar os pesos de todos os estados, que podem ser astronomicamente
/// muitos. A receita e uma linha: proponha um vizinho, aceite com probabilidade
/// igual a razao dos pesos limitada a um, e fique onde esta se recusar.
///
/// A razao dos pesos CANCELA a soma que ninguem consegue calcular, e esse e o
/// truque inteiro. Aqui da para conferir isso em igualdade exata: com pesos na
/// casa de 10^20, a estacionaria da cadeia construida e exatamente a
/// distribuicao normalizada, e nenhum lugar do codigo calcula a normalizacao.
/// </summary>
public static class Metropolis
{
    /// <summary>
    /// Monta a cadeia de Metropolis para os pesos dados, com proposta uniforme
    /// entre todos os outros estados.
    ///
    /// A proposta e simetrica, que e o caso original de Metropolis: a razao de
    /// propostas some e sobra so a razao de pesos.
    /// </summary>
    public static Cadeia Montar(params BigInteger[] pesos)
    {
        ArgumentNullException.ThrowIfNull(pesos);

        var n = pesos.Length;
        if (n < 2) throw new ArgumentException("sao precisos pelo menos dois estados", nameof(pesos));

        var m = new Fracao[n, n];
        var proposta = new Fracao(1, n - 1);

        for (var i = 0; i < n; i++)
        {
            var ficar = Fracao.Zero;

            for (var j = 0; j < n; j++)
            {
                if (i == j) continue;

                // min(1, peso_j / peso_i), a aceitacao de Metropolis.
                var razao = new Fracao(pesos[j], pesos[i]);
                var aceita = razao > Fracao.Um ? Fracao.Um : razao;

                m[i, j] = proposta * aceita;
                ficar += proposta * (Fracao.Um - aceita);
            }

            // Recusar nao e desperdicio: ficar parado E o passo. Sem isso as
            // linhas nao somam um e a cadeia nem existe.
            m[i, i] = ficar;
        }

        return new Cadeia("metropolis", m);
    }

    /// <summary>A distribuicao alvo normalizada, so para comparar no fim.</summary>
    public static Fracao[] Alvo(params BigInteger[] pesos)
    {
        ArgumentNullException.ThrowIfNull(pesos);

        var total = BigInteger.Zero;
        foreach (var peso in pesos) total += peso;

        return pesos.Select(peso => new Fracao(peso, total)).ToArray();
    }

    /// <summary>
    /// O EQUILIBRIO DETALHADO: pi_i * P_ij tem que ser igual a pi_j * P_ji, para
    /// todo par.
    ///
    /// Ele e a razao de Metropolis funcionar, e e uma condicao mais forte que
    /// ser estacionaria: toda cadeia que satisfaz o equilibrio detalhado tem
    /// aquela distribuicao como estacionaria, e a volta e FALSA. O ciclo de
    /// quatro estados tem estacionaria uniforme e nao tem equilibrio detalhado
    /// nenhum, porque ele so anda para frente.
    /// </summary>
    public static bool EquilibrioDetalhado(Cadeia cadeia, Fracao[] distribuicao)
    {
        ArgumentNullException.ThrowIfNull(cadeia);
        ArgumentNullException.ThrowIfNull(distribuicao);

        for (var i = 0; i < cadeia.Estados; i++)
            for (var j = 0; j < cadeia.Estados; j++)
                if (distribuicao[i] * cadeia.Transicoes[i, j] != distribuicao[j] * cadeia.Transicoes[j, i])
                    return false;

        return true;
    }

    /// <summary>
    /// Quanto do passo e gasto RECUSANDO, somado sobre a estacionaria.
    ///
    /// E o preco da receita, e ele cresce quando os pesos sao muito desiguais: a
    /// cadeia passa a maior parte do tempo parada no estado pesado, recusando
    /// propostas de ir para os leves.
    /// </summary>
    public static Fracao TaxaDeRecusa(Cadeia cadeia)
    {
        ArgumentNullException.ThrowIfNull(cadeia);

        var estacionaria = Exata.Estacionaria(cadeia);
        if (estacionaria is null) return Fracao.Zero;

        var soma = Fracao.Zero;
        for (var i = 0; i < cadeia.Estados; i++) soma += estacionaria[i] * cadeia.Transicoes[i, i];

        return soma;
    }
}
