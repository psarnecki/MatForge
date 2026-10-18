# Koncepcja projektu pracy inżynierskiej

**Tytuł:** Projekt i implementacja systemu wspomagającego analizę rozwoju zawodników sportów chwytanych

**Tytuł (EN):** Design and implementation of a system supporting progress analysis of grappling sports athletes

---

## Wstępna idea

Projekt zakłada stworzenie systemu webowego będącego osobistym narzędziem zawodnika sportów chwytanych (ze szczególnym uwzględnieniem brazylijskiego jiu-jitsu). Użytkownik tworzy konto jako zawodnik, prowadzi dziennik treningowy w formie kalendarza, rejestruje przebieg treningów i startów w zawodach, zapisuje cele treningowe oraz materiały techniczne. Zebrane dane - w szczególności z dziennika treningowego, rejestru sparingów i modułu zawodów - stanowią podstawę modułu analizy, który przekłada je na statystyki i wnioski dotyczące rozwoju zawodnika.

Głównym celem systemu jest stworzenie uporządkowanego narzędzia, które pomaga zawodnikowi gromadzić dane treningowe w jednym miejscu i przekładać je na użyteczne, mierzalne informacje o własnym rozwoju sportowym. Monitorowanie treningów pełni tu rolę źródła danych, natomiast analiza rozwoju - rolę głównego rezultatu użytkowego. Dzięki temu zawodnik może świadomiej kierować własnym treningiem, co przekłada się na szybszy i bardziej ukierunkowany postęp umiejętności oraz lepsze przygotowanie do startów w zawodach.

W skrócie, celem pracy inżynierskiej jest zaprojektowanie i implementacja systemu do monitorowania treningów oraz analizy rozwoju zawodnika.

---

## Problem

W ostatnich latach popularność brazylijskiego jiu-jitsu bardzo wzrosła. Widać to zarówno po największych wydarzeniach sportowych (rosnąca liczba publiczności oraz chętnych do rywalizacji), jak i na klubowych matach, gdzie przybywa młodych osób, zawodników, a także hobbistów zainteresowanych dyscypliną. Trend ten zauważyły również największe federacje mieszanych sportów walki na świecie, tworząc własne odłamy dedykowane tej dyscyplinie (np. UFC i UFC BJJ).

Wraz z rosnącą liczbą trenujących rośnie też potrzeba świadomego śledzenia własnego rozwoju. Zawodnicy sportów walki często zapisują swoje treningi, cele, wyniki sparingów, linki do interesujących technik oraz refleksje po zawodach w wielu różnych miejscach: notatniku, kalendarzu, arkuszach kalkulacyjnych, aplikacjach mobilnych lub wiadomościach do siebie. Takie podejście utrudnia analizę postępów, wyciąganie wniosków i planowanie dalszego rozwoju.

W przypadku sportów chwytanych szczególnie istotne są informacje takie jak:

- regularność treningów,
- typy odbytych jednostek treningowych,
- charakter treningów (np. technika, zadaniówki, sparingi),
- bilans sparingów,
- najczęściej stosowane techniki kończące,
- techniki, przez które zawodnik najczęściej przegrywa,
- wyniki startów w zawodach,
- cele treningowe i stopień ich realizacji.

Brak jednego, uporządkowanego narzędzia sprawia, że zawodnik może mieć ogromną trudność z rzetelną oceną swoich mocnych i słabych stron oraz z planowaniem dalszego treningu.

---

## Cel projektu

Celem projektu jest zaprojektowanie i implementacja systemu webowego, który umożliwi użytkownikowi:

- prowadzenie konta z profilem zawodnika,
- prowadzenie dziennika treningowego w formie kalendarza,
- rejestrowanie charakteru treningów oraz ich rezultatów (m.in. bilansu sparingów),
- zapisywanie wyników startów w zawodach,
- definiowanie i śledzenie celów treningowych,
- gromadzenie materiałów technicznych do nauki,
- rejestrowanie pomiarów ciała (np. waga, wzrost, % tkanki tłuszczowej) w regularnych odstępach czasu i prezentowanie ich na wykresach w czasie,
- analizowanie postępów i stylu zawodnika na podstawie zgromadzonych danych metodami statystyczno-regułowymi.

Istotnym elementem systemu ma być moduł analizy danych treningowych i startowych, pozwalający identyfikować najczęściej stosowane techniki, najczęstsze przyczyny porażek oraz zmiany skuteczności w czasie. Implementacja obejmuje analizę statystyczno-regułową - agregacje, udziały procentowe, trendy czasowe oraz reguły wnioskowania o mocnych i słabych stronach zawodnika.

---

## Zakres funkcjonalny

### 1. Konto użytkownika i profil zawodnika

Użytkownik zakłada konto jako zawodnik i uzupełnia podstawowe dane:

- imię,
- nazwisko,
- klub lub kluby sportowe, do których przynależy (możliwość wskazania więcej niż jednego),
- stopnie pasów - zawodnik może wpisać kilka pasów w zależności od dyscypliny (np. pas BJJ oraz pas judo).

### 2. Kalendarz i dziennik treningowy

System udostępnia kalendarz, w którym użytkownik zapisuje swoje codzienne treningi. Treningi dzielą się na dwie główne sekcje:

- **trening sportów chwytanych**,
- **inne treningi** - gdzie można wybrać rodzaj aktywności, np. siłownia, motoryka, bieganie, rower itd.

#### Trening sportów chwytanych

Dla treningu sportów chwytanych użytkownik może zdefiniować:

- **charakter treningu**, np. trening techniczny, zadaniówki, sparingi, joga dla BJJ i tym podobne,
- **dyscyplinę** treningu (np. BJJ, zapasy, judo, sambo),
- **formułę / odmianę** zależną od dyscypliny (np. dla BJJ: gi / no-gi; dla zapasów: styl klasyczny / wolny; dla judo i innych dyscyplin - odpowiednie warianty),
- **klub**, w którym odbył się trening - wybór z listy klubów przypisanych do profilu zawodnika lub opcja „inny” (bez wpisywania nazwy),
- **czas trwania treningu**,
- **samopoczucie**,
- **własne notatki**, w których można zapisać obserwacje.

Dyscyplina i formuła powinny być modelowane jako powiązane słowniki - każda dyscyplina ma przypisany zestaw dostępnych formuł/odmian. Dzięki temu lista wyboru formuły dopasowuje się do wybranej dyscypliny, a dane pozostają spójne i gotowe do agregacji.

Pole klubu przy treningu pozwala odróżnić jednostki treningowe odbywające się w różnych miejscach i umożliwia statystyki typu liczba treningów lub czas treningu w poszczególnych klubach. Użytkownik wybiera klub z listy zdefiniowanej w profilu albo opcję „inny”, gdy trening odbył się poza tymi klubami (np. seminarium lub trening gościnny). W analizie prezentowany jest rozkład wg klubów z profilu oraz osobna kategoria „inny”. Szczegóły takiego treningu (np. nazwa klubu lub miejsca) użytkownik może zapisać w notatkach, jeśli uzna to za potrzebne.

Najważniejszym aspektem jest możliwość zapisania, co udało się na treningu osiągnąć, w szczególności w zakresie sparingów:

- liczba odbytych rund sparingowych,
- bilans rund: wygrane / przegrane,
- sposób zakończenia rundy, jeśli wystąpił (np. technika poddania), zapisywany zarówno dla wygranej, jak i przegranej rundy.

#### Inne treningi

Dla treningów spoza sportów chwytanych zakres danych jest prostszy i obejmuje m.in. rodzaj aktywności, czas trwania, samopoczucie oraz notatki.

Pozostałe szczegóły (np. partnerzy treningowi, prowadzący, kontuzje, liczba drillów, intensywność treningu) zawodnik może zapisywać ręcznie w notatkach treningu, jeśli czuje taką potrzebę.

### 3. Rejestr sparingów i ich rezultatów

Rejestr sparingów jest częścią treningu i stanowi jeden z kluczowych elementów projektu, ponieważ dostarcza danych do analizy stylu zawodnika. Wstępny zakres danych dla rundy sparingowej:

- wynik rundy (wygrana / przegrana / remis lub runda bez rozstrzygnięcia),
- sposób zakończenia, jeśli wystąpił (technika poddania),
- strona zakończenia (czy to zawodnik poddał, czy został poddany),
- opcjonalnie pozycja, z której wynikła akcja kończąca (np. plecy, dosiad, garda),
- opcjonalna notatka.

Technika oraz pozycja zakończenia powinny pochodzić ze słowników (osobnych zbiorów wartości), a nie być wpisywane jako dowolny tekst. Dzięki temu dane da się później agregować i przeliczać na statystyki (np. „70% poddań pochodzi z pleców”). Pole pozycji może być opcjonalne, aby formularz pozostał wygodny do uzupełniania bezpośrednio po treningu.

Przedstawiony zakres pól ma charakter wstępny i może ulec drobnemu doprecyzowaniu na etapie projektowania formularza oraz modelu danych.

### 4. Moduł analizy zawodnika

System generuje statystyki oraz podsumowania rozwoju zawodnika, np. w ujęciu miesięcznym. Analiza obejmuje m.in.:

- liczbę treningów w wybranym okresie,
- łączny czas treningów oraz podział na sekcje (chwytane / inne),
- udział treningów wg dyscypliny (np. BJJ, zapasy, judo) oraz formuły/odmiany (np. gi / no-gi, styl klasyczny / wolny),
- rozkład treningów wg klubów z profilu oraz kategorii „inny”,
- bilans sparingów,
- najczęściej używane techniki kończące (wraz z udziałem procentowym),
- techniki, przez które zawodnik najczęściej przegrywa,
- zmianę skuteczności w czasie,
- dominujące typy akcji,
- mocne i słabe strony zawodnika - wykrywane na podstawie reguł (np. techniki, przez które zawodnik przegrywa częściej niż określony próg procentowy).

Moduł analizy uwzględnia zarówno dane treningowe, jak i dane z zawodów, a także analizuje je osobno (np. osobno skuteczność na treningach i osobno na zawodach), co pozwala porównać dyspozycję treningową ze startową.

Implementacja opiera się na metodach statystyczno-regułowych: agregacji danych ze słowników, obliczaniu udziałów procentowych, trendów czasowych oraz prostych reguł biznesowych generujących wnioski dla zawodnika. Takie podejście jest adekwatne do skali danych gromadzonych przez pojedynczego użytkownika i pozwala na rzetelną, weryfikowalną analizę bez konieczności posiadania dużego zbioru danych treningowych.

### 5. Cele treningowe

Użytkownik może definiować cele treningowe, np.:

- wykonanie określonej liczby treningów w miesiącu,
- odbycie określonej liczby sparingów,
- przećwiczenie wybranej techniki,
- przygotowanie do zawodów,
- poprawa obrony przed konkretnym typem poddania,
- nauka konkretnego poddania/techniki.

Część celów może być powiązana z danymi wprowadzanymi do dziennika treningowego, dzięki czemu system będzie mógł automatycznie aktualizować postęp ich realizacji. W praktyce oznacza to, że cele mierzalne, oparte na danych, które i tak trafiają do systemu, mogą być przeliczane automatycznie. Przykłady:

- cel „20 treningów w tym miesiącu” - system zlicza treningi z bieżącego miesiąca i pokazuje postęp (np. 12/20),
- cel „100 rund sparingowych w miesiącu” - system sumuje rundy z zapisanych treningów,
- cel „przygotowanie do zawodów do dnia X” - system pokazuje odliczanie i powiązaną aktywność w okresie przygotowań.

Cele trudniej mierzalne (np. „poprawa obrony przed duszeniem zza pleców”) mogą być wspierane pośrednio - np. przez pokazanie, jak zmienia się liczba przegranych przez dany typ poddania w czasie - lub aktualizowane ręcznie przez użytkownika. Dzięki temu moduł celów łączy dane z konkretną, widoczną informacją zwrotną.

### 6. Biblioteka technik i materiałów

System umożliwia zapisywanie linków do materiałów technicznych, np. z YouTube, TikToka czy Instagram Reels, tak aby wartościowe nagrania z interesującymi technikami nie zostały zgubione. Każdy materiał może posiadać m.in.:

- tytuł,
- adres URL,
- źródło,
- kategorię techniki,
- status.

Przykładowe statusy:

- zapisane,
- obejrzane,
- przećwiczone,
- opanowane.

Moduł rozwiązuje praktyczny problem gubienia wartościowych materiałów treningowych i pozwala śledzić, które techniki zostały faktycznie przećwiczone.

### 7. Moduł zawodów

Użytkownik może ręcznie dodawać starty w zawodach wraz z informacją o przebiegu. Dla zawodów można zapisać m.in.:

- nazwę zawodów,
- datę,
- lokalizację,
- kategorie (wiekową, zaawansowania, wagową),
- dyscyplinę oraz formułę/odmianę (np. BJJ gi / no-gi, zapasy w stylu klasycznym / wolnym),
- ogólny wynik startu.

Dla pojedynczej walki można zapisać:

- przeciwnika,
- wynik (wygrana / przegrana),
- sposób zakończenia (technika poddania, wynik punktowy, decyzja itp.),
- czas walki,
- własne notatki.

Dane z zawodów zasilają moduł analizy, dzięki czemu system może analizować skuteczność startową oddzielnie od treningowej. Na tym etapie nie zakłada się pobierania zawodów z zewnętrznego API, ponieważ w świecie sportów chwytanych nie istnieje jedno uniwersalne i łatwo dostępne źródło takich danych.

### 8. Pomiary ciała

Prosta funkcjonalność umożliwiająca użytkownikowi regularne (np. raz w miesiącu) zapisywanie parametrów ciała, takich jak:

- waga,
- wzrost,
- % tkanki tłuszczowej,
- inne wybrane parametry (do ustalenia).

System prezentuje zebrane dane na wykresach w czasie, co pozwala zawodnikowi obserwować zmiany sylwetki i parametrów fizycznych w kontekście treningu oraz przygotowań startowych.

---

## Możliwe kierunki dalszego rozwoju

Poniższe elementy nie wchodzą w zakres implementacji pracy inżynierskiej, lecz mogą stanowić naturalne rozszerzenie systemu w przyszłości:

- **uczenie maszynowe** - możliwy kierunek rozwoju modułu analizy przy większym i bardziej zróżnicowanym zbiorze danych treningowych (np. wielu użytkowników lub długim okresie obserwacji).
- **dziennik wydarzeń** - pamiętnik z wpisami upamiętniającymi ważne dni (np. starty w zawodach), ze zdjęciami i opisami; forma dokumentacji osobistej, bez wpływu na moduł analizy,
- **raporty PDF** - eksport miesięcznych podsumowań statystyk treningowych i startowych do pliku,
- **tagi techniczne przy treningu** - np. „leglocki”, „passing”, „guard retention”,

---

## Stos technologiczny

- **backend:** ASP.NET Core Web API (C#),
- **dostęp do danych / ORM:** Entity Framework Core,
- **baza danych:** Microsoft SQL Server,
- **mapowanie DTO:** Riok.Mapperly (mapper oparty na generatorach źródeł),
- **walidacja danych:** FluentValidation (złożone reguły biznesowe) wspierana wbudowanymi atrybutami DataAnnotations (proste reguły),
- **frontend:** React (z biblioteką do wykresów, np. Chart.js lub Recharts) do prezentacji dashboardów i statystyk,
- **autoryzacja i uwierzytelnianie:** ASP.NET Core Identity; sposób utrzymania sesji (własne tokeny JWT, gotowe Identity API endpoints lub uwierzytelnianie oparte o cookies) do ustalenia na etapie projektowania,
- **dokumentacja API:** Swagger / OpenAPI,
- **testy:** xUnit / FluentAssertions / Moq (testy jednostkowe i integracyjne logiki biznesowej oraz obliczeń statystycznych),
- **konteneryzacja i środowisko uruchomieniowe:** Docker / Docker Compose,
- **kontrola wersji:** Git / GitHub.

Architektura systemu zakłada podział na warstwy (API, logika biznesowa, warstwa dostępu do danych), co ułatwia testowanie oraz utrzymanie kodu. Ostateczny dobór technologii zostanie dokładnie doprecyzowany po ustaleniu wszystkich szczegółów implementacyjnych.

---

## Wartość inżynierska projektu

Projekt nie ogranicza się do prostych operacji typu CRUD. Jego istotą jest zaprojektowanie modelu danych dla domeny sportów chwytanych oraz przetworzenie gromadzonych danych treningowych i startowych na użyteczne, mierzalne informacje dla zawodnika.

Najważniejsze elementy inżynierskie:

- zaprojektowanie spójnego modelu danych dla złożonej domeny (treningi, sparingi, techniki, zawody, walki, cele),
- implementacja wielowarstwowego systemu webowego z uwierzytelnianiem i autoryzacją użytkowników,
- realizacja dziennika treningowego oraz rejestru sparingów i walk,
- opracowanie modułu analizy danych opartego na metodach statystyczno-regułowych (agregacje, udziały procentowe, trendy w czasie, reguły wykrywania mocnych i słabych stron),
- mechanizm automatycznego przeliczania postępu celów na podstawie danych z dziennika,
- prezentacja wyników w formie czytelnych dashboardów i wykresów,
- pokrycie kluczowej logiki biznesowej (zwłaszcza obliczeń statystycznych) testami automatycznymi.

Dzięki temu praca obejmuje pełny cykl inżynierski: analizę problemu, projekt rozwiązania, implementację, testowanie oraz prezentację wyników w użytecznej formie.
