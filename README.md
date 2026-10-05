# markov

Cadeias de Markov do zero em C# e .NET 8: distribuição estacionária, tempo de
mistura, tempos de retorno, PageRank e Metropolis-Hastings. O juiz é a **fração
de inteiro grande**, então a estacionária tem denominador inteiro e as
igualdades são igualdades.

```
$ dotnet medidor.dll estacionaria

tempo           irredutivel   aperiodica    5/6  1/6
linha de 5      irredutivel   aperiodica    1/5  1/5  1/5  1/5  1/5
pegajosa        irredutivel   aperiodica    1/2  1/2
ciclo de 4      irredutivel   periodo 4     1/4  1/4  1/4  1/4
duas ilhas      redutivel     aperiodica    nao e unica
```

A estacionária da cadeia do tempo é **5/6 e 1/6**, não `0,8333333333333334` e
`0,16666666666666666`. Ter a resposta certa é o que permite dizer que a iteração
chegou nela, em vez de só ter parado de mudar.

## A cadeia que tem estacionária e nunca converge

A linha do ciclo de quatro é o contraexemplo mais útil do repositório. Ele é
irredutível, tem estacionária uniforme e ela é única.
E a iteração de potência nunca chega nela:

```
$ dotnet medidor.dll periodica

    passo  0     0.7500
    passo  1     0.7500
    passo  2     0.7500
    ...
    passo  9     0.7500
```

A distância fica em `0,75` para sempre, porque a distribuição visita os quatro
estados em rodízio e nunca se espalha. "Irredutível" não basta: falta
**aperiódica**.

A cura padrão é a cadeia **preguiçosa**, que com metade de probabilidade fica
onde está. Ela tem a mesma estacionária, em igualdade exata, passa a ter período
um e converge em 78 passos. E tem preço:

```
  cadeia             mistura antes   mistura depois
  tempo                          2                4
  linha de 5                     5                9
  pegajosa                     347              693
```

A preguiça deixa a cadeia mais **lenta**: os autovalores viram `(1 + lambda)/2`,
então o segundo sobe. O que ela conserta é o menos um, que impedia a
convergência.

## A velocidade é o segundo autovalor, e ele é medido

```
$ dotnet medidor.dll velocidade

cadeia             lambda medido    lambda exato   relaxamento   mistura
tempo                   0.400001             2/5           1.7         2
linha de 5              0.809017               -           5.2         5
pegajosa                0.998000         499/500         500.0       347
```

Nenhuma linha dessa tabela calcula autovalor. A medida só olha por quanto a
distância até a estacionária encolhe a cada passo, e para as cadeias de dois
estados o número bate com a fórmula fechada `1 - p - q`.

```
  limiar    passos
 0.25000       347
 0.02500     1,497
 0.00250     2,647
 0.00025     3,797
```

Cada fator de dez na exigência custa **1.150 passos a mais**, sempre o mesmo
número. O custo cresce com o logaritmo da exigência, porque a distância cai por
um fator constante a cada passo.

## O teorema de Kac, em igualdade e não em aproximação

```
$ dotnet medidor.dll kac

cadeia            estado    pi exato   retorno medio   produto
tempo                  0         5/6             6/5         1
tempo                  1         1/6               6         1
linha de 5             0         1/5               5         1
pegajosa               0         1/2               2         1
ciclo de 4             0         1/4               4         1
```

Um estado que o passeio visita **um sexto** do tempo tem retorno médio de **seis**
passos. A última coluna é `1` em todas as linhas, e não `0,9999999999999998`,
porque a conta inteira é feita em fração.

E os tempos de chegada mostram o desperdício do passeio aleatório:

```
    do estado 0 ate o 4:   20
    do estado 1 ate o 4:   18
    do estado 2 ate o 4:   14
    do estado 3 ate o 4:    8
```

Atravessar uma linha de **cinco** estados custa vinte passos em média, e dobrar o
tamanho da linha multiplica a travessia por quatro.

## Metropolis: amostrar sem conhecer a normalização

```
$ dotnet medidor.dll metropolis

  pesos:         1  7  100  3
  alvo:          1/111  7/111  100/111  1/37
  estacionaria:  1/111  7/111  100/111  1/37

  igualdade exata: True
```

Nenhum lugar do código soma os pesos para normalizar. A razão entre dois pesos
**cancela** a soma que ninguém consegue calcular, e é esse cancelamento que
permite amostrar de uma distribuição com mais estados do que dá para enumerar.
Com pesos na casa de `10^41`, a igualdade continua exata.

O preço é ficar parado recusando:

```
  pesos                         taxa de recusa   chance de sair
  1, 1, 1, 1                          0.000000        1.00E+000
  1, 2, 4, 8                          0.511111        4.89E-001
  1, 10, 100, 1000                    0.926193        7.38E-002
  1, 1, 1, 1000000                    0.999996        4.00E-006
```

Com um peso um milhão de vezes maior, a chance de sair do estado pesado é de
**um em duzentos e cinquenta mil** por passo. A receita continua certa, e fica cara
exatamente onde a distribuição é mais torta, que é onde se costuma precisar
dela.

## O que as medidas me corrigiram

**Eu esperava que a razão de convergência do PageRank fosse igual ao
amortecimento, e ela fica abaixo dele, às vezes muito abaixo.** Na corrente com
amortecimento `0,99`, a medida dá `0,70`: o limite erra por quase trinta pontos.
O teorema é um **limite**, não uma previsão, e é exatamente por ser um limite que
ele vale a pena, porque dá para prometer a velocidade sem olhar o grafo.

```
$ dotnet medidor.dll pagerank

grafo                 amortecimento   razao medida    passos
corrente de 8                  0.50       0.276837        31
corrente de 8                  0.85       0.650286        70
corrente de 8                  0.99       0.695336       112
estrela de 8                   0.50       0.437501        35
estrela de 8                   0.85       0.743750        94
duas ilhas de 4                0.99       0.924918       322
```

**O piso da medida do segundo autovalor estava baixo demais, e isso virou ruído
disfarçado de número.** Com o piso em `1e-13`, a cadeia do tempo media `0,40045`
em vez de `0,4`. A essa altura a distância já é tão pequena que o próprio
arredondamento domina a diferença, e a razão passa a medir o arredondamento. Com
o piso em `1e-10` ela bate com a fórmula fechada até a nona casa. Medir perto do
zero não é medir melhor.

**Eu escrevi que um terço mais um terço mais um terço dá `1,0000000000000002` em
ponto flutuante, e dá exatamente um.** O arredondamento de `1/3` para baixo é
compensado na soma. O caso em que o `double` falha de verdade é outro, e o teste
passou a usar ele: `0,1 + 0,2` não é `0,3`. A conferência exata das linhas da
cadeia se justifica por esse, não por aquele.

**E escrevi uma linha de cadeia "errada" que somava um.** O teste que deveria
provar que uma linha inválida é recusada usava `999999/1000000 + 1/1000000`, que
é exatamente um: o teste passava pelo motivo errado, provando que a cadeia aceita
linhas válidas. Com `999998` ele passou a provar o que dizia.

**Estacionária não implica equilíbrio detalhado, e o ciclo prova isso em fração.**
A implicação vale só numa direção: toda cadeia com equilíbrio detalhado tem
aquela distribuição como estacionária. O ciclo de quatro só anda para frente, tem
estacionária uniforme e não está em equilíbrio entre par nenhum de estados.

## Por que o juiz é a fração

Distribuição estacionária, tempo de retorno e PageRank são todos solução de um
sistema linear com coeficientes racionais. Em racional eles têm resposta exata, e
em ponto flutuante a soma das probabilidades dá `0,9999999999999998`: não dá para
separar erro de método de erro de arredondamento.

Com a fração, "o método iterativo converge para a resposta certa?" deixa de ser
uma comparação com tolerância e vira uma comparação com a resposta. E o teorema
de Kac vira igualdade, não aproximação.

Nenhuma medida aqui usa relógio. Todas são contagem de passos ou comparação
exata, então a saída é a mesma no Linux, no Windows e no macOS, e a integração
contínua roda nos três.

## As peças

| arquivo | o que faz |
| --- | --- |
| `Fracao.cs` | a fração de inteiro grande, que é o juiz |
| `Cadeia.cs` | a cadeia, a irredutibilidade e o período |
| `Exata.cs` | a estacionária exata e a distância de variação total |
| `Potencia.cs` | a iteração de potência e o segundo autovalor medido |
| `Mistura.cs` | o tempo de mistura e a cadeia preguiçosa |
| `Tempos.cs` | tempos de chegada e de retorno, e o teorema de Kac |
| `Pagerank.cs` | os dois remendos e a garantia de velocidade |
| `Metropolis.cs` | amostrar sem conhecer a normalização |

## Como rodar

```
dotnet test testes/Markov.Testes/Markov.Testes.csproj -c Release
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- tudo
```

As medidas aceitam `estacionaria`, `velocidade`, `periodica`, `kac`, `pagerank`,
`metropolis` e `tudo`.

## Licença

MIT.
