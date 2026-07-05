using FishFocus.Interfaces;
using FishFocus.Shared.Models;

namespace FishFocus.Repositories;

public class FakeFishRepository : IFishRepository
{
    private readonly List<Fish> _fakeDb = new()
    {
        new Fish
        {
            Name = "Карась",
            Description = "Неприхотливая и очень живучая рыба, способная обитать даже в заросших прудах. Славится своим умением выживать в самых суровых условиях.",
            Rarity = 1,
            Points = 5,
            MinMinutes = 1,
            ImageUrl = "/images/fish/common_carp.png"
        },
        new Fish
        {
            Name = "Окунь",
            Description = "Активный полосатый хищник, который охотится стаями. Его легко узнать по характерным черным полосам и ярко-красным плавникам.",
            Rarity = 1,
            Points = 10,
            MinMinutes = 10,
            ImageUrl = "images/fish/perch.png"
        },
        new Fish
        {
            Name = "Красноперка",
            Description = "Красивая мирная рыба с яркой золотистой чешуей. Предпочитает тихие участки водоемов с обилием кувшинок и водной растительности.",
            Rarity = 1,
            Points = 15,
            MinMinutes = 30,
            ImageUrl = "images/fish/rudd.png"
        },
        new Fish
        {
            Name = "Язь",
            Description = "Осторожная и сильная рыба, обитающая в реках с чистой водой. Взрослые особи становятся очень скрытными и предпочитают глубокие места.",
            Rarity = 2,
            Points = 30,
            MinMinutes = 45,
            ImageUrl = "images/fish/ide.png"
        },
        new Fish
        {
            Name = "Карп",
            Description = "Могучая рыба, которую за ее аппетит часто называют «водяным поросенком». Пользуется огромным уважением среди рыболовов за сильное сопротивление.",
            Rarity = 2,
            Points = 40,
            MinMinutes = 60,
            ImageUrl = "images/fish/mirror_carp.png"
        },
        new Fish
        {
            Name = "Щука",
            Description = "Грозный хищник, мастер молниеносных атак из засады. Ее вытянутое тело и острые как бритва зубы делают ее опасным противником.",
            Rarity = 2,
            Points = 50,
            MinMinutes = 75,
            ImageUrl = "images/fish/pike.png"
        },
        new Fish
        {
            Name = "Сом",
            Description = "Гигант речных глубин с длинными усами, ведущий ночной образ жизни. Этот хищник предпочитает отдыхать в самых глубоких омутах и ямах.",
            Rarity = 2,
            Points = 70,
            MinMinutes = 100,
            ImageUrl = "images/fish/catfish.png"
        },
        new Fish
        {
            Name = "Марлин",
            Description = "Легендарный океанский хищник, способный развивать невероятную скорость. Его мощное тело и нос-копье — мечта любого профессионального рыболова.",
            Rarity = 3,
            Points = 300,
            MinMinutes = 119,
            ImageUrl = "images/fish/marlin.png"
        }
    };

    public Task<List<Fish>> GetAllFishesAsync()
    {
        return Task.FromResult(_fakeDb);
    }
}