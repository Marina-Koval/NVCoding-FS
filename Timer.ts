

export class  Timer {
    private duration:number; 
    private interval:number;    
    private counter:number;

    private tickcallback: Function;
    private endcallback: Function;
    
    constructor(
        duration: number,
        endcallback: Function,
        tickcallback: Function = null,
        interval: number = 1000) {            
            this.duration = duration;
            this.endcallback = endcallback;            
            this.interval = interval;
            this.tickcallback = tickcallback;
    }

    public start():void {     
        this.counter = setInterval(() => {            
            if(this.isOver()) this.stop();
            else this.countdown();
        }, this.interval)
    }
    
    public stop():void {
        clearInterval(this.counter);
        this.endcallback();
    }
    
    private isOver():boolean {
        return this.duration <= 0;
    }

    private countdown():void {
        this.duration -= this.interval;
        this.tickcallback(this.toDefaultTimerFormat());
    }

    private toDefaultTimerFormat() {
        let time = new Date(0,0,0,0,0,0, this.duration);

        let h = time.getHours() > 9 ? time.getHours() : "0" + time.getHours();
        let m = time.getMinutes() > 9 ? time.getMinutes() : "0" + time.getMinutes();
        let s = time.getSeconds() > 9 ? time.getSeconds() : "0" + time.getSeconds();

        return `${h}:${m}:${s}`;
    }
}
